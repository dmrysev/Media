module Media.UI.Core.LocalMedia.Browser

open Media
open Media.UI.Core
open Media.UI.Core
open Media.UI.Core.Command
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive
open System
open System.Threading
open System.Threading.Tasks

type OutputViewChoice = Thumbnails | Preview

type LimitResult = { StartIndex: int; MaxResultCount: int }

type ThumbnailsViewModel(
    localMediaPreview: API.LocalMedia.Preview,
    fileSystem: API.FileSystem,
    resources: API.Resource,
    selectedIndex: NotifyValue<int>, 
    contextMenuItems: MenuItemViewModel list) =
    inherit UI.Core.ViewModelBase()
    let defaultThumbnailBytes = resources.ReadImageBytes "overview.png"
    member val SelectedIndex = selectedIndex
    member val MenuItems = contextMenuItems
    member val Thumbnails = Seq.empty<ImageViewModel> with get,set
    member val ThumbnailSize = 200 with get,set
    member val ThumbnailHeight = 200 with get,set
    member val ThumbnailWidth = 200 with get,set
    member this.OnThumbnailSizeChanged() =
        this.ThumbnailHeight <- this.ThumbnailSize
        this.ThumbnailWidth <- this.ThumbnailSize
    member this.LoadThumbnails mediaIds = 
        this.Thumbnails <-
            mediaIds
            |> Seq.map (fun id -> 
                let getBytes() =
                    try 
                        localMediaPreview.ReadThumbnailBytes id
                        |> Option.defaultValue defaultThumbnailBytes
                    with ex -> 
                        API.Diagnostics.except ex
                        defaultThumbnailBytes
                ImageViewModel (getBytes, this.ThumbnailSize) )
    member this.Clear() = this.Thumbnails <- []

type PreviewViewModel(localMediaPreview: API.LocalMedia.Preview) =
  inherit UI.Core.ViewModelBase()
    member val Previews = Task.Run (fun _ -> Seq.empty<ImageViewModel>) with get,set
    member this.Load mediaId = 
        this.Previews <- Task.Run(fun _ ->
            try 
                localMediaPreview.ReadScreenshotsBytes mediaId
                |> Seq.map (fun bytes -> 
                    let getBytes() = bytes
                    ImageViewModel (getBytes, 200) ) 
            with error -> 
                API.Diagnostics.except error
                Seq.empty )
    member this.Clear() = this.Previews <- Task.Run (fun _ -> Seq.empty)
    member this.SetMediaId (id: Media.Id option) =
        this.Clear()
        match id with
        | Some id -> this.Load id
        | None -> ()

type State = {
    Items: Media.Id seq
    PerPageLimit: LimitResult
    Page: int
    SelectedItemIndex: int }
with static member Default = {
        Items = []
        PerPageLimit = { StartIndex = 0; MaxResultCount = 9 }
        Page = 0
        SelectedItemIndex = -1 }

type MainViewModel(
    localMedia: API.LocalMedia,
    fileSystem: API.FileSystem,
    resources: API.Resource) as this =
    inherit UI.Core.ViewModelBase()
    let selectedItemIndex = NotifyValue -1
    let selectedMediaId = NotifyValue<Media.Id option> None
    let searchResultContextMenuItems: MenuItemViewModel list = [
        { MenuItemViewModel.Default with Header = "Open"; Command = initCommand this.Open }
        { MenuItemViewModel.Default with Header = "Open all"; Command = initCommand this.OpenAll } ]
    let thumbnailsViewModel = ThumbnailsViewModel (
        localMedia.Preview,
        fileSystem,
        resources,
        selectedItemIndex,
        searchResultContextMenuItems)
    let mutable previousViewModelResources: IDisposable option = None
    do 
        selectedItemIndex.Changed.Add (fun index ->
            selectedMediaId.Value <-
                if index = -1 || index > (this.ItemsPerPage |> Util.Seq.lastIndex) then None
                else
                    this.ItemsPerPage 
                    |> Seq.item index
                    |> Some)

    member val IsFocused = false with get,set

    member this.SetSelectedItemIndex (index: int) = 
        if this.ItemsPerPage |> Seq.isEmpty then
            selectedItemIndex.Value <- -1
        elif index = -1 && this.ItemsPerPage |> Seq.isEmpty |> not then 
            selectedItemIndex.Value <- 0
        elif index <= (this.ItemsPerPage |> Util.Seq.lastIndex) then
            selectedItemIndex.Value <- index
    member this.GetSelectedItemIndex() = selectedItemIndex.Value

    member val SelectedMediaId = selectedMediaId.AsObservableValue()

    member val ThumbnailsViewModel = thumbnailsViewModel
    member val OutputViewModel: ViewModelBase = thumbnailsViewModel with get,set

    member val MaxPerPage = 9 with get,set
    member this.OnMaxPerPageChanged() = this.UpdatePerPageLimit()

    member val Page = 0 with get,set
    member this.OnPageChanged() = 
        this.UpdatePerPageLimit()
        if this.ItemsPerPage |> Seq.isEmpty |> not then 
            this.SetSelectedItemIndex 0
        else this.SetSelectedItemIndex -1
    member this.NextPage() = 
        let nextPageItemsCount =
            let startIndex = (this.Page + 1) * this.MaxPerPage
            Util.Seq.limitItems startIndex this.MaxPerPage this.Items
            |> Seq.length
        if nextPageItemsCount <> 0 then
            this.Page <- this.Page + 1
    member this.PreviousPage() = 
        if this.Page <> 0 then
            this.Page <- this.Page - 1

    member val OutputView = Thumbnails with get,set
    member this.OnOutputViewChanged() =
        match previousViewModelResources with
        | Some resources -> 
            resources.Dispose()
            previousViewModelResources <- None
        | None -> ()
        match this.OutputView with
        | Thumbnails -> 
            this.OutputViewModel <- thumbnailsViewModel
        | Preview -> 
            let viewModel = PreviewViewModel(localMedia.Preview)
            previousViewModelResources <- 
                selectedMediaId.Changed
                |> Observable.subscribe viewModel.SetMediaId
                |> Some
            viewModel.SetMediaId selectedMediaId.Value
            this.OutputViewModel <- viewModel
    member this.SetThumbnailsView() = this.OutputView <- Thumbnails
    member this.SetPreviewView() = this.OutputView <- Preview

    member val PerPageLimit = State.Default.PerPageLimit with get,set
    member this.OnPerPageLimitChanged() = this.UpdateItemsPerPage()
    member this.UpdatePerPageLimit() =
        let startIndex = this.Page * this.MaxPerPage
        this.PerPageLimit <- { StartIndex = startIndex; MaxResultCount = this.MaxPerPage }

    member val Items = Seq.empty<Media.Id> with get,set
    member this.OnItemsChanged() = this.UpdateItemsPerPage()
    member this.Delete() =
        match selectedMediaId.Value with
        | Some id -> 
            localMedia.Data.Delete id
            this.Items <- this.Items |> Util.Seq.removeItem id
        | None -> ()
    member this.Clear() = this.Items <- Seq.empty

    member val ItemsPerPage = Seq.empty<Media.Id> with get,set
    member this.OnItemsPerPageChanged() = this.LoadThumbnails()
    member this.UpdateItemsPerPage() =
        this.ItemsPerPage <- Util.Seq.limitItems this.PerPageLimit.StartIndex this.PerPageLimit.MaxResultCount this.Items

    member this.LoadThumbnails() =
        let selectedItemIndex = this.GetSelectedItemIndex()
        this.ItemsPerPage
        |> thumbnailsViewModel.LoadThumbnails
        this.SetSelectedItemIndex selectedItemIndex

    member val SearchEvent = Event<API.LocalMedia.FindOptions>()
    member val FindOptions = API.LocalMedia.FindOptions.Default with get,set
    member this.Search (findOptions: API.LocalMedia.FindOptions) (page: int) = 
        this.SearchEvent.Trigger findOptions
        this.Items <- Seq.empty
        this.Items <- 
            localMedia.MetaData.FindIds findOptions
            |> Seq.toArray
        this.Page <- page
    
    member this.PreviousItem() =
        if selectedItemIndex.Value - 1 < 0 && this.Page <> 0 then
            this.PreviousPage()
            this.SetSelectedItemIndex (this.ItemsPerPage |> Util.Seq.lastIndex)
        elif selectedItemIndex.Value - 1 >= 0 then
            this.SetSelectedItemIndex (selectedItemIndex.Value - 1)
    member this.NextItem() =
        if selectedItemIndex.Value + 1 >= this.MaxPerPage then
            this.NextPage()
        elif selectedItemIndex.Value + 1 < Seq.length this.ItemsPerPage then
            this.SetSelectedItemIndex (selectedItemIndex.Value + 1)

    member this.Open() = 
        this.ItemsPerPage 
        |> Seq.item selectedItemIndex.Value
        |> localMedia.Data.OpenOne
    member this.OpenAll() =
        this.ItemsPerPage 
        |> localMedia.Data.OpenMany
    member this.OpenNext() = Async.Start(async {
        if selectedItemIndex.Value < this.MaxPerPage - 1 then
            this.SetSelectedItemIndex (selectedItemIndex.Value + 1)
        else this.Page <- this.Page + 1
        this.Open() })

let initSessionManager (viewModel: MainViewModel) (appDataDirPath: DirectoryPath) (fileSystem: API.FileSystem) =
    let stateDataAccess =
        let stateDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryPath "MediaBrowser/state")
        Core.FileSystem.GenericDataAccess.initWithStringId<State> stateDirPath fileSystem
    let loadState state = 
        viewModel.Items <- state.Items
        viewModel.PerPageLimit <- state.PerPageLimit
        viewModel.Page <- state.Page
        viewModel.SetSelectedItemIndex (state.SelectedItemIndex)
    let manager: API.Session.Manager = {
        Load = fun name ->
            match stateDataAccess.TryRead name with
            | Some state -> loadState state
            | None -> ()
        Save = fun name ->
            let state: State = {
                Items = viewModel.Items 
                PerPageLimit = viewModel.PerPageLimit
                Page = viewModel.Page
                SelectedItemIndex = viewModel.GetSelectedItemIndex() }
            stateDataAccess.Write name state
        Delete = fun name -> stateDataAccess.EnsureDeleted name
        Reset = fun _ -> loadState State.Default       }
    manager

let initCommandsGroup 
    (viewModel: MainViewModel) 
    (localMedia: API.LocalMedia) 
    (fileSystem: API.FileSystem)
    (commonTags: string seq)
    (commonLocations: DirectoryPath seq) =
    let commonCharacters =
        localMedia.MetaData.ReadAll()
        |> Seq.collect (fun metaData -> metaData.Characters )
        |> Seq.distinct
        |> Seq.sort
        |> Seq.cache
    let commonAuthors =
        localMedia.MetaData.ReadAll()
        |> Seq.collect (fun metaData -> metaData.Authors )
        |> Seq.distinct
        |> Seq.sort
        |> Seq.cache
    let commonLanguages =
        localMedia.MetaData.ReadAll()
        |> Seq.collect (fun metaData -> metaData.Languages )
        |> Seq.distinct
        |> Seq.sort
        |> Seq.cache
    let commonExportOutputLocations = 
        localMedia.Export.ReadCommonOutputLocations()
        |> Seq.map DirectoryPath.value
        |> Seq.cache
    let find = { 
        Command.Init "Find" (fun options ->
            viewModel.OutputView <- Thumbnails
            let args = options.CommandArguments
            let findOptions = { 
                API.LocalMedia.FindOptions.Default with
                    Filter = {| 
                        API.LocalMedia.FindOptions.Default.Filter with
                            Tags = tryFindValues args "Tags"
                            TagsAnyOf = tryFindValues args "TagsAnyOf"
                            ExcludeTags = tryFindValues args "ExcludeTags"
                            Locations = 
                                match tryFindValues args "Locations" with
                                | Some locations -> locations |> Seq.map DirectoryPath |> Some
                                | None -> None
                            TitleContains = tryFindValue args "TitleContains"
                            Authors = tryFindValues args "Authors"
                            Characters = tryFindValues args "Characters" 
                            Languages = tryFindValues args "Languages"
                            PathType = tryFindUnionValue<API.LocalMedia.PathType> args "PathType" |}
                    Sort = 
                        tryFindUnionValue<API.LocalMedia.SortOptions> args "Sort" 
                        |> Option.defaultValue API.LocalMedia.SortOptions.DefaultSort }
            let page = findValueOrDefault args "Page" "0" |> int
            viewModel.Search findOptions page )
        with 
            ArgumentDefinitions = [ 
                { CommandArgumentDefinition.Init "Tags" with 
                    Suggestions = fun options -> commonTags }
                { CommandArgumentDefinition.Init "TagsAnyOf" with 
                    Suggestions = fun options -> commonTags }
                { CommandArgumentDefinition.Init "ExcludeTags" with 
                    Suggestions = fun options -> commonTags }
                { CommandArgumentDefinition.Init "Locations" with 
                    Suggestions = fun _ -> commonLocations |> Seq.map DirectoryPath.value }
                { CommandArgumentDefinition.Init "Authors" with 
                    Suggestions = fun _ -> commonAuthors }
                { CommandArgumentDefinition.Init "Characters" with 
                    Suggestions = fun _ -> commonCharacters }
                { CommandArgumentDefinition.Init "Languages" with 
                    Suggestions = fun _ -> commonLanguages }
                { CommandArgumentDefinition.Init "PathType" with 
                    Suggestions = fun _ -> Util.Reflection.Union.casesStrings<API.LocalMedia.PathType>() }
                CommandArgumentDefinition.Init "TitleContains"
                { CommandArgumentDefinition.Init "Sort" with
                    Suggestions = fun _ -> Util.Reflection.Union.casesStrings<API.LocalMedia.SortOptions>() }
                CommandArgumentDefinition.Init "Page" ]
            AsyncExecutionEnabled = true     }
    let findById =
        { Command.Init "FindById" (fun options ->
            viewModel.OutputView <- Thumbnails
            let args = options.CommandArguments
            let id = findUnionValue<Media.Id> args "Id"
            viewModel.Items <- [ id ])
            with ArgumentDefinitions = [ 
                { CommandArgumentDefinition.Init "Id" with
                    Position = Some 0 } ]}
    let nextPage = Command.Init "NextPage" (fun _ -> viewModel.NextPage())
    let previousPage = Command.Init "PreviousPage" (fun _ -> viewModel.PreviousPage())
    let setPage =
        { Command.Init "SetPage" (fun options -> 
                match tryFindValue options.CommandArguments "Page" with
                | Some page -> 
                    viewModel.Page <- page |> int
                | None -> raise (ArgumentException())) 
            with ArgumentDefinitions = [ 
                { CommandArgumentDefinition.Init "Page" with 
                    Position = Some 0 } ] }
    let openMedia = 
        { Command.Init "Open" (fun options -> 
            let args = options.CommandArguments
            if isFlagSet args "All" then
                localMedia.Data.OpenMany viewModel.Items
            elif isFlagSet args "CurrentPage" then
                localMedia.Data.OpenMany viewModel.ItemsPerPage
            else
                viewModel.SelectedMediaId.Value |> Option.get
                |> localMedia.Data.OpenOne ) 
            with ArgumentDefinitions = [ 
                CommandArgumentDefinition.Init "All"
                CommandArgumentDefinition.Init "CurrentPage" ] }
    let openNext = Command.Init "OpenNext" (fun _ -> viewModel.OpenNext())
    let deleteMedia = {
        Command.Init "Delete" (fun options -> 
            let args = options.CommandArguments
            if isFlagSet args "All" then
                viewModel.Items
                |> Seq.iter localMedia.Data.Delete
                viewModel.Clear()
            else viewModel.Delete())
        with ArgumentDefinitions = [
            CommandArgumentDefinition.Init "All" ]   }
    let setThumbnailSize = 
        { Command.Init "SetThumbnailSize" (fun options -> 
            viewModel.ThumbnailsViewModel.ThumbnailSize <- findValue options.CommandArguments "Size" |> int )
            with ArgumentDefinitions = [
                { CommandArgumentDefinition.Init "Size" with 
                    Position = Some 0 } ]}
    let setMaxPerPage = 
        { Command.Init "SetMaxPerPage" (fun options -> 
            viewModel.MaxPerPage <- findValue options.CommandArguments "Count" |> int )
            with ArgumentDefinitions = [
                { CommandArgumentDefinition.Init "Count" with 
                    Position = Some 0 } ]}
    let thumbnails = Command.Init "Thumbnails" (fun _ -> viewModel.OutputView <- Thumbnails)
    let preview = Command.Init "Preview" (fun _ -> viewModel.OutputView <- Preview)
    let pathsToPlaylist =
        { Command.Init "ToPlaylist" (fun options -> 
            let args = options.CommandArguments
            let outputDirPath = findValue args "OutputLocation" |> DirectoryPath
            let outputFileName = 
                findValueOrDefault args "OutputName" "playlist"
                |> FileName
            let isShuffleSet = isFlagSet args "Shuffle"
            let outputFilePath = outputDirPath/outputFileName
            viewModel.Items
            |> Seq.map (fun id -> 
                let metaData = localMedia.MetaData.FindById id
                metaData.Path |> Util.Path.value )
            |> fun paths -> 
                if isShuffleSet then Util.Seq.shuffle paths
                else paths
            |> fileSystem.File.WriteLines outputFilePath ) 
            with ArgumentDefinitions = [ 
                { CommandArgumentDefinition.Init "OutputLocation" with 
                    Suggestions = fun _ -> commonExportOutputLocations }
                CommandArgumentDefinition.Init "OutputName"
                CommandArgumentDefinition.Init "Shuffle" ] }        
    let commandsGroup: CommandsGroup = {
        Name = "MediaBrowser"
        Commands = [ 
            find; findById; setPage; nextPage; previousPage; 
            openMedia; deleteMedia; openNext; setThumbnailSize; setMaxPerPage;
            thumbnails; preview; pathsToPlaylist ] }
    commandsGroup
