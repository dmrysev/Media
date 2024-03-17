module Media.Application.UI.Core.MainWindow

open Media
open Media.API
open Media.Core
open Media.UI.Core
open Util.Path
open System

type Focus = CommandPrompt | CommandPromptAlias | MainContentFocus
type MainContent = MediaBrowser | FileSystemBrowser | InfoOutput | ComicBook | ImageSet

type MainViewModel (dep: API.Application.Dependency, getMainViewSize, finalize: IObservable<int>) as this =
    inherit UI.Core.ViewModelBase()

    let appDataDirPath = dep.AppDataDirPath
    let fileSystem = dep.FileSystem
    let fileSystemEnc = dep.FileSystemEnc
    let localMedia = dep.LocalMedia
    let remoteMedia = dep.RemoteMedia
    let resource = dep.Resource

    let geometryChanged = Event<Util.Drawing.Size>()
    let finalizeEvent = finalize |> Observable.map (fun _ -> ())

    let newInfoMesssage = new Event<string>()
    let infoOutputViewModel = new Diagnostics.InfoOutput.MainViewModel (newInfoMesssage.Publish, geometryChanged.Publish)
    let sendInfo = newInfoMesssage.Trigger

    let fileSystemBrowserViewModel = FileSystem.Browser.MainViewModel (fileSystem, resource)
    let mediaBrowserViewModel = LocalMedia.Browser.MainViewModel(localMedia, fileSystem, resource)

    let imageSetViewModel = 
        let viewModel = ImageSet.MainViewModel (getMainViewSize, fileSystem, resource)
        viewModel.FocusRequest.Publish.Add this.FocusImageSet
        viewModel

    let commonTags =
        Seq.concat [
            localMedia.MetaData.ReadFavoriteTags()
            localMedia.MetaData.ReadCommonTags() ]
        |> Seq.distinct
        |> Seq.cache
    let commonLocations = localMedia.MetaData.ReadCommonLocations() |> Seq.cache

    let sessionManagers = [
        LocalMedia.Browser.initSessionManager mediaBrowserViewModel appDataDirPath fileSystem
        ImageSet.initSessionManager imageSetViewModel appDataDirPath fileSystem
        FileSystem.Browser.initSessionManager fileSystemBrowserViewModel appDataDirPath fileSystem ]

    let getSelectedMediaMetaData() =
        match mediaBrowserViewModel.SelectedMediaId.Value with
        | Some id -> localMedia.MetaData.FindById id
        | None -> raise (ArgumentException "No media item selected")

    let tryFindMetaDataByPathOrNew (path: Path) =
        localMedia.MetaData.ReadAll()
        |> Seq.tryFind (fun metaData -> metaData.Path = path )
        |> Option.defaultValue (MetaData.New (Guid.NewGuid()) (path))

    let getSelectedMetaData() =
        match this.CurrentMainContent with
        | MediaBrowser -> getSelectedMediaMetaData()
        | FileSystemBrowser -> 
            fileSystemBrowserViewModel.GetSelectedItem()
            |> tryFindMetaDataByPathOrNew
        | ImageSet ->
            imageSetViewModel.GetCurrentImagePath()
            |> Path.File
            |> tryFindMetaDataByPathOrNew
        | _ -> raise (ArgumentException "No media item selected")

    let getSelectedPath() =
        match this.CurrentMainContent with
        | MediaBrowser -> 
            let metaData = getSelectedMediaMetaData()
            metaData.Path
        | FileSystemBrowser -> fileSystemBrowserViewModel.GetSelectedItem()
        | ImageSet -> imageSetViewModel.ImageSetDirPath |> Util.Path.Directory
        | _ -> raise (ArgumentException "No media item selected")

    let getAllPaths() =
        match this.CurrentMainContent with
        | MediaBrowser -> 
            mediaBrowserViewModel.Items
            |> Seq.map (fun mediaId -> 
                let metaData = localMedia.MetaData.FindById mediaId
                metaData.Path )
        | FileSystemBrowser -> fileSystemBrowserViewModel.Items
        | _ -> raise (ArgumentException "No media item selected")

    let getSelectedMediaId() =
        match this.CurrentMainContent with
        | MediaBrowser -> 
            match mediaBrowserViewModel.SelectedMediaId.Value with
            | Some id -> id
            | None -> raise (ArgumentException "No media item selected")
        | _ -> raise (ArgumentException "No media item selected")

    let commandsGroups = [
        LocalMedia.MetaData.initCommandsGroup getSelectedMetaData localMedia.MetaData.Write localMedia.MetaData.AddFavoriteTag sendInfo this.FocusInfoOutput commonTags fileSystem
        LocalMedia.Export.initCommandsGroup getSelectedPath localMedia.Export commonLocations sendInfo
        LocalMedia.Format.initCommandsGroup getSelectedPath localMedia.Data
        LocalMedia.Browser.initCommandsGroup mediaBrowserViewModel localMedia fileSystem commonTags commonLocations
        LocalMedia.SlideShow.initCommandsGroup getSelectedPath localMedia.Data.InitVideoSlideShow imageSetViewModel.NextImage
        RemoteMedia.initCommandsGroup remoteMedia getSelectedMediaId localMedia.MetaData.ReadAll
        ImageSet.initCommandsGroup imageSetViewModel getSelectedPath getAllPaths commonTags
        FileSystem.Browser.initCommandsGroup fileSystemBrowserViewModel getSelectedPath fileSystem
        Diagnostics.InfoOutput.initCommandsGroup infoOutputViewModel
        Session.initCommandsGroup appDataDirPath fileSystem sessionManagers sendInfo this.FocusInfoOutput finalizeEvent ]
    let commandsGroups = 
        commandsGroups 
        |> Seq.append [
            CommandPromptAlias.initCommandsGroup commandsGroups fileSystem appDataDirPath ]
    let commandPromptViewModel = CommandPrompt.MainViewModel (commandsGroups, fileSystem, appDataDirPath, getMainViewSize, sendInfo)
    let commandPromptAliasViewModel = CommandPromptAlias.MainViewModel (fileSystem, appDataDirPath)

    let comicBookViewModel = ComicBook.MainViewModel (appDataDirPath, getMainViewSize, fileSystem)

    do
        commandPromptViewModel.CommandExecuted.Publish.Add (fun _ ->
            this.FocusMainContent() )

        commandPromptAliasViewModel.NewCommandInput.Publish.Add (fun command ->
            this.FocusCommandPrompt() 
            commandPromptViewModel.SetInput command )

        fileSystemBrowserViewModel.FocusRequest.Publish.Add (fun _ -> 
            this.CurrentMainContent <- FileSystemBrowser)

        comicBookViewModel.ScreenTapped.Publish.Add this.OnScreenTapped

    member val GeometryChanged = geometryChanged.Publish with get

    member val Width = 600.0 with get,set
    member this.OnWidthChanged() =
        geometryChanged.Trigger { Width = this.Width; Height = this.Height }
    member val Height = 600.0 with get,set
    member this.OnHeightChanged() =
        geometryChanged.Trigger { Width = this.Width; Height = this.Height }
    
    member val ComicBook = comicBookViewModel with get,set

    member val CurrentFocus = CommandPrompt with get,set
    member this.SetFocus (focus: Focus) =
        this.CurrentFocus <- focus
        this.UpdateView()

    member this.UpdateView() =
        this.IsCommandPromptVisible <- false
        commandPromptViewModel.IsFocused <- false
        this.IsCommandPromptAliasVisible <- false
        commandPromptAliasViewModel.IsFocused <- false
        mediaBrowserViewModel.IsFocused <- false
        fileSystemBrowserViewModel.IsFocused <- false
        infoOutputViewModel.IsFocused <- false
        comicBookViewModel.IsFocused <- false
        imageSetViewModel.IsFocused <- false
        match this.CurrentFocus with
        | CommandPrompt -> 
            this.IsCommandPromptVisible <- true
            commandPromptViewModel.Focus()
        | CommandPromptAlias ->
            this.IsCommandPromptAliasVisible <- true
            commandPromptAliasViewModel.Focus()
        | MainContentFocus ->
            match this.CurrentMainContent with
            | MediaBrowser -> mediaBrowserViewModel.IsFocused <- true
            | FileSystemBrowser -> fileSystemBrowserViewModel.IsFocused <- true
            | InfoOutput -> infoOutputViewModel.IsFocused <- true
            | ComicBook -> comicBookViewModel.IsFocused <- true
            | ImageSet -> imageSetViewModel.IsFocused <- true
    member val MainContent: ViewModelBase = mediaBrowserViewModel with get,set
    member val CurrentMainContent = MediaBrowser with get,set
    member this.OnCurrentMainContentChanged() =
        match this.CurrentMainContent with
        | MediaBrowser -> this.MainContent <- mediaBrowserViewModel
        | FileSystemBrowser -> this.MainContent <- fileSystemBrowserViewModel
        | InfoOutput -> this.MainContent <- infoOutputViewModel
        | ComicBook -> this.MainContent <- comicBookViewModel
        | ImageSet -> this.MainContent <- imageSetViewModel
    member this.FocusMainContent() =
        this.SetFocus MainContentFocus

    member val CommandPrompt = commandPromptViewModel with get,set
    member val IsCommandPromptVisible = true with get,set
    member this.FocusCommandPrompt() = 
        this.SetFocus CommandPrompt

    member val CommandPromptAlias = commandPromptAliasViewModel with get,set
    member val IsCommandPromptAliasVisible = false with get,set
    member this.FocusCommandPromptAlias() = 
        this.SetFocus CommandPromptAlias

    member this.FocusMediaBrowser() = 
        this.CurrentMainContent <- MediaBrowser
        this.SetFocus MainContentFocus
    member this.MediaBrowserPreviousItem() = mediaBrowserViewModel.PreviousItem()
    member this.MediaBrowserNextItem() = mediaBrowserViewModel.NextItem()

    member this.FocusFileSystemBrowser() = 
        this.CurrentMainContent <- FileSystemBrowser
        this.SetFocus MainContentFocus

    member this.FocusInfoOutput() = 
        this.CurrentMainContent <- InfoOutput
        this.SetFocus MainContentFocus

    member this.FocusImageSet() =
        this.CurrentMainContent <- ImageSet
        this.SetFocus MainContentFocus

    member this.OnScreenTapped (pos: Util.Drawing.Point) = 
        let mainViewSize = getMainViewSize()
        let screenWidth = mainViewSize.Width
        let screenHeight = mainViewSize.Height
        if pos.X < screenWidth * 0.2 && pos.Y < screenHeight * 0.2 then
            this.FocusCommandPrompt()
        else this.FocusMainContent()
