module Media.UI.Core.FileSystem.Browser

open Media
open Media
open Media.UI.Core
open Media.UI.Core.Command
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive

type FileSystemEntry = {
    Thumbnail: ImageViewModel
    Name: string }

type LimitResult = { StartIndex: int; MaxResultCount: int }

type State = {
    Directory: DirectoryPath
    PerPageLimit: LimitResult
    Page: int
    SelectedItemIndex: int }
with static member Default = {
        Directory = DirectoryPath.None
        PerPageLimit = { StartIndex = 0; MaxResultCount = 9 }
        Page = 0
        SelectedItemIndex = -1 }

type MainViewModel (
    fileSystem: API.FileSystem,
    resources: API.Resource) as this = 
    inherit UI.Core.ViewModelBase()

    let defaultFileThumbnailBytes = resources.ReadImageBytes "overview.png"
    let initFileThumbnail (filePath: FilePath) = 
        let getBytes() =
            try 
                if filePath |> FilePath.hasImageExtension then 
                    fileSystem.File.ReadBytes filePath
                else defaultFileThumbnailBytes
            with ex -> 
                API.Diagnostics.except ex
                defaultFileThumbnailBytes 
        ImageViewModel (getBytes, this.EntrySize)

    let dirThumbnailBytes = resources.ReadImageBytes "folder_icon.png"
    let initDirThumbnail () = 
        let getBytes() = dirThumbnailBytes
        ImageViewModel (getBytes, this.EntrySize)

    let mutable currentDirectory = State.Default.Directory
    
    member this.CurrentDirectory with get () = currentDirectory

    member val FocusRequest = Event<unit>()
    member val IsFocused = false with get,set

    member val FileSystemEntries = Seq.empty<FileSystemEntry> with get,set
    member val EntrySize = 200 with get,set
    member val EntryHeight = 200 with get,set
    member val EntryWidth = 200 with get,set
    member this.OnEntrySizeChanged() =
        this.EntryHeight <- this.EntrySize
        this.EntryWidth <- this.EntrySize
        this.UpdateItemsPerPage()

    member val SelectedIndex = -1 with get,set
    member val SelectedIndexes = Seq.empty<int> with get,set
    member this.SetSelectedIndexes indexes =
        this.SelectedIndexes <- indexes
    member this.SetSelectedIndex (index: int) = 
        if this.ItemsPerPage |> Seq.isEmpty then
            this.SelectedIndex <- -1
        elif index = -1 && this.ItemsPerPage |> Seq.isEmpty |> not then 
            this.SelectedIndex <- 0
        elif index <= (this.ItemsPerPage |> Util.Seq.lastIndex) then
            this.SelectedIndex <- index
    member this.PreviousItem() =
        if this.SelectedIndex - 1 < 0 && this.Page <> 0 then
            this.PreviousPage()
            this.SetSelectedIndex (this.ItemsPerPage |> Util.Seq.lastIndex)
        else this.SetSelectedIndex (this.SelectedIndex - 1)
    member this.NextItem() =
        if this.SelectedIndex + 1 >= this.MaxPerPage then
            this.NextPage()
        else this.SetSelectedIndex (this.SelectedIndex + 1)

    member val Page = 0 with get,set
    member this.OnPageChanged() = 
        this.UpdatePerPageLimit()
        this.SetSelectedIndex 0
    member this.NextPage() = this.Page <- this.Page + 1
    member this.PreviousPage() = this.Page <- this.Page - 1

    member val MaxPerPage = 9 with get,set
    member this.OnMaxPerPageChanged() = this.UpdatePerPageLimit()
    
    member val PerPageLimit = State.Default.PerPageLimit with get,set
    member this.OnPerPageLimitChanged() = this.UpdateItemsPerPage()
    member this.UpdatePerPageLimit() =
        let startIndex = this.Page * this.MaxPerPage
        this.PerPageLimit <- { StartIndex = startIndex; MaxResultCount = this.MaxPerPage }

    member val Items = Seq.empty<Path> with get,set
    member this.OnItemsChanged() = 
        this.UpdateItemsPerPage()

    member val ItemsPerPage = Seq.empty<Path> with get,set
    member this.OnItemsPerPageChanged() = 
        this.FileSystemEntries <-
            this.ItemsPerPage
            |> Seq.map (fun path ->
                match path with
                | File filePath ->
                    let fileSystemEntry: FileSystemEntry = {
                        Thumbnail = initFileThumbnail filePath
                        Name = 
                            filePath 
                            |> FilePath.fileName 
                            |> FileName.value }
                    fileSystemEntry
                | Directory dirPath ->
                    let fileSystemEntry: FileSystemEntry = {
                        Thumbnail = initDirThumbnail()
                        Name = 
                            dirPath 
                            |> DirectoryPath.directoryName
                            |> DirectoryName.value }
                    fileSystemEntry  )
    member this.UpdateItemsPerPage() =
        this.ItemsPerPage <- Util.Seq.limitItems this.PerPageLimit.StartIndex this.PerPageLimit.MaxResultCount this.Items
    member this.GetSelectedItem() = this.ItemsPerPage |> Seq.item this.SelectedIndex

    member this.SetPaths (paths: Path seq) = 
        this.Items <- 
            [ paths
              |> Seq.choose (fun path -> match path with Directory _ -> Some path | _ -> None)
              |> Seq.sortBy Util.Path.value
              paths
              |> Seq.choose (fun path -> match path with File _ -> Some path | _ -> None)
              |> Seq.sortBy Util.Path.value ]
            |> Seq.concat
    member this.BrowseDirectory (dirPath: DirectoryPath) =
        fileSystem.Directory.ListEntries dirPath
        |> this.SetPaths
        this.Page <- 0
        currentDirectory <- dirPath
    member this.NavigateBackward = initCommand (fun _ ->
        let parentDirPath = currentDirectory |> DirectoryPath.parent
        fileSystem.Directory.ListEntries parentDirPath
        |> this.SetPaths
        this.Page <-
            let index = this.Items |> Util.Seq.findItemIndex (Path.Directory currentDirectory)
            index / this.PerPageLimit.MaxResultCount
        this.SelectedIndex <-
            this.ItemsPerPage |> Util.Seq.findItemIndex (Path.Directory currentDirectory)
        currentDirectory <- parentDirPath   )
    member this.OpenSelected = initCommand (fun _ ->
        if this.SelectedIndexes |> Seq.length = 1 then
            match this.ItemsPerPage |> Seq.item this.SelectedIndex with
            | File path -> fileSystem.File.Open path
            | Directory path -> this.BrowseDirectory path
        else 
            this.SelectedIndexes
            |> Seq.choose (fun index -> 
                let path = this.ItemsPerPage |> Seq.item index
                match path with 
                | File path -> Some path 
                | _ -> None)
            |> Seq.iter fileSystem.File.Open       )

    member this.DeleteSelected = initCommand (fun _ ->
        this.SelectedIndexes
        |> Seq.map (fun index -> this.ItemsPerPage |> Seq.item index)
        |> Seq.iter fileSystem.FileSystemEntry.MoveToTrashBin
        fileSystem.Directory.ListEntries currentDirectory
        |> this.SetPaths    )

    member this.Clear() =
        this.SetPaths Seq.empty
        currentDirectory <- DirectoryPath.None

let initSessionManager (viewModel: MainViewModel) (appDataDirPath: DirectoryPath) (fileSystem: API.FileSystem) =
    let stateDataAccess =
        let stateDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryPath "FileSystemBrowser/state")
        Core.FileSystem.GenericDataAccess.initWithStringId<State> stateDirPath fileSystem
    let loadState state =
        if state.Directory = DirectoryPath.None then
            viewModel.Clear()
        else
            viewModel.BrowseDirectory state.Directory
        viewModel.PerPageLimit <- state.PerPageLimit
        viewModel.Page <- state.Page
        viewModel.SetSelectedIndex state.SelectedItemIndex
    let manager: API.Session.Manager = {
        Load = fun name ->
            match stateDataAccess.TryRead name with
            | Some state -> loadState state
            | None -> ()
        Save = fun name ->
            let state: State = {
                Directory = viewModel.CurrentDirectory
                PerPageLimit = viewModel.PerPageLimit
                Page = viewModel.Page
                SelectedItemIndex = viewModel.SelectedIndex }
            stateDataAccess.Write name state
        Delete = fun name -> stateDataAccess.EnsureDeleted name
        Reset = fun _ -> loadState State.Default       }
    manager

let initCommandsGroup 
    (viewModel: MainViewModel)
    (getSelectedPath: unit -> Path)
    (fileSystem: API.FileSystem) =
    let openSelected = Command.Init "OpenSelected" (fun _ -> 
        match getSelectedPath() with
        | File path -> 
            path
            |> FilePath.directoryPath
            |> viewModel.BrowseDirectory
        | Directory path -> viewModel.BrowseDirectory path
        viewModel.FocusRequest.Trigger() )  
    let browseDirectory = { 
        Command.Init "BrowseDirectory" (fun options -> 
            viewModel.FocusRequest.Trigger()
            let args = options.CommandArguments
            let dirPath = 
                findValue args "DirectoryPath" 
                |> Util.String.removeLastCharacterIfEquals "/"
                |> DirectoryPath
            viewModel.BrowseDirectory dirPath ) 
        with ArgumentDefinitions = [{ 
            CommandArgumentDefinition.Init "DirectoryPath" with 
                Position = Some 0
                SuggestionsOptions = { 
                    SuggestionsOptions.Default with
                        CustomFilterEnabled = true
                        AutoAppendValueOnAccept = "/" }
                Suggestions = fun options -> 
                    let args = options.InputArguments
                    match tryFindValue args "DirectoryPath" with
                    | Some dirPath ->
                        let dirPath = DirectoryPath dirPath
                        if dirPath.Value |> Util.String.endsWith "/" then
                            fileSystem.Directory.ListDirectories dirPath
                            |> Seq.map DirectoryPath.value
                        else
                            let inputDirName = 
                                dirPath
                                |> DirectoryPath.directoryName
                                |> DirectoryName.value
                            dirPath 
                            |> DirectoryPath.parent
                            |> fileSystem.Directory.ListDirectories
                            |> Seq.filter (fun dirPath -> 
                                let dirName =
                                    dirPath
                                    |> DirectoryPath.directoryName
                                    |> DirectoryName.value
                                dirName |> Util.String.contains inputDirName )
                            |> Seq.map DirectoryPath.value
                    | None -> [] }      ]   }
    let setMaxPerPage = 
        { Command.Init "SetMaxPerPage" (fun options -> 
            viewModel.MaxPerPage <- findValue options.CommandArguments "Count" |> int )
            with ArgumentDefinitions = [
                { CommandArgumentDefinition.Init "Count" with 
                    Position = Some 0 } ]}
    let deleteSelected = { 
        Command.Init "DeleteSelected" (fun options -> 
            viewModel.DeleteSelected.Execute() ) 
        with ArgumentDefinitions = [ ] }
    let commandsGroup: CommandsGroup = {
        Name = "FileSystemBrowser"
        Commands = [ openSelected; browseDirectory; setMaxPerPage; deleteSelected ] }
    commandsGroup
