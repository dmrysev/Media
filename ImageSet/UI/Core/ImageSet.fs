module Media.UI.Core.ImageSet

open Media
open Media.UI.Core
open Media.UI.Core.Command
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive
open System

type State = {
    Images: FilePath seq
    ImageIndex: int }
with static member Default = {
        Images = []
        ImageIndex = -1 }

type MainViewModel(
    getMainViewSize: unit -> Util.Drawing.Size,
    fileSystem: API.FileSystem,
    resources: API.Resource) =
    inherit UI.Core.ViewModelBase()

    let getDefaultImageBytes() = resources.ReadImageBytes "overview.png"

    member val CurrentImage = ImageViewModel (getDefaultImageBytes, 100) with get,set

    member val ScreenTapped = Event<Util.Drawing.Point>() with get
    member val IsFocused = false with get,set
    member val FocusRequest = Event<unit>()

    member val ImageFilePaths = Seq.empty<FilePath> with get,set
    member val ImageIndex = -1 with get,set
    member this.OnImageIndexChanged() =
        let getBytes() =
            this.ImageFilePaths 
            |> Seq.item this.ImageIndex
            |> fileSystem.File.ReadBytes
        let mainViewSize = getMainViewSize()
        let imageSize = mainViewSize.Height |> int
        this.CurrentImage <- ImageViewModel (getBytes, imageSize, Interpolation = None)
        this.FitImage()
    member this.NextImage() =
        if this.ImageIndex + 1 < (this.ImageFilePaths |> Seq.length) then
            this.ImageIndex <- this.ImageIndex + 1
    member this.PreviousImage() =
        if this.ImageIndex - 1 >= 0 then this.ImageIndex <- this.ImageIndex - 1
    member this.ForwardImageJump(count: int) =
        if this.ImageIndex + count < (this.ImageFilePaths |> Seq.length) then
            this.ImageIndex <- this.ImageIndex + count
    member this.BackwardImageJump(count: int) =
        this.ImageIndex <-
            if this.ImageIndex - count < 0 then 0
            else this.ImageIndex - count

    member val ImageRotation: double = 0.0 with get,set
    member this.RotateRight = initCommand (fun _ -> this.ImageRotation <- this.ImageRotation + 90.0)
    member this.RotateLeft = initCommand (fun  _ -> this.ImageRotation <- this.ImageRotation - 90.0)

    member val ImagePositionX: double = 0.0 with get,set
    member val ImagePositionY: double = 0.0 with get,set
    member this.MoveLeft = initCommand (fun _ -> this.ImagePositionX <- this.ImagePositionX - 100.0)
    member this.MoveRight = initCommand (fun _ -> this.ImagePositionX <- this.ImagePositionX + 100.0)
    member this.MoveUp = initCommand (fun _ -> this.ImagePositionY <- this.ImagePositionY - 100.0)
    member this.MoveDown = initCommand (fun _ -> this.ImagePositionY <- this.ImagePositionY + 100.0)

    member val ImageWidth: double = 100.0 with get,set
    member val ImageHeight: double = 100.0 with get,set

    member val ImageScale: double = 1.0 with get,set
    member this.OnImageScaleChanged() =
        this.ImageWidth <- this.ImageWidth * this.ImageScale
        this.ImageHeight <- this.ImageHeight * this.ImageScale
    member this.ZoomIn = initCommand (fun _ -> this.ImageScale <- this.ImageScale + 0.2)
    member this.ZoomOut = initCommand (fun _ -> this.ImageScale <- this.ImageScale - 0.2)

    member this.FitImage() = 
        let mainViewSize = getMainViewSize()
        let screenWidth = mainViewSize.Width
        let screenHeight = mainViewSize.Height
        this.ImageScale <- 1
        this.ImagePositionX <- 0.0
        this.ImagePositionY <- 0.0
        this.ImageWidth <- screenWidth
        this.ImageHeight <- screenHeight

    member this.SetImages (imagePaths: FilePath seq) =
        this.ImageFilePaths <- 
            imagePaths
            |> Seq.filter FilePath.hasImageExtension
            |> Seq.cache
        this.ImageIndex <- 0

    member val ImageSetDirPath = DirectoryPath.None with get,set
    member this.OpenImageSet (imageSetDirPath: DirectoryPath) =
        this.ImageSetDirPath <- imageSetDirPath
        fileSystem.Directory.ListFiles this.ImageSetDirPath
        |> Seq.sortBy (fun path -> path.Value)
        |> this.SetImages
        this.FocusRequest.Trigger()

    member val IsMenuVisible = false with get,set

    member this.OnScreenTapped (pos: Util.Drawing.Point) = 
        this.ScreenTapped.Trigger pos
        if this.IsMenuVisible then this.IsMenuVisible <- false
        else 
            let mainViewSize = getMainViewSize()
            let screenWidth = mainViewSize.Width
            let screenHeight = mainViewSize.Height
            if pos.X > screenWidth * 0.8 && pos.Y < screenHeight * 0.2 then 
                this.ForwardImageJump 10
            elif pos.X > screenWidth * 0.2 && pos.X < screenWidth * 0.8 && pos.Y < screenHeight * 0.2 then
                this.IsMenuVisible <- true
            elif pos.X > screenWidth / 2.0 then
                this.NextImage()
            elif pos.X < screenWidth / 2.0 then
                this.PreviousImage()

    member this.GetCurrentImagePath() =
        this.ImageFilePaths 
        |> Seq.item this.ImageIndex

    member this.Delete = initCommand(fun _ ->
        this.GetCurrentImagePath()
        |> fileSystem.File.MoveToTrashBin
        this.NextImage()    )

let initSessionManager (viewModel: MainViewModel) (appDataDirPath: DirectoryPath) (fileSystem: API.FileSystem) =
    let stateDataAccess =
        let stateDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryPath "ImageSet/state")
        Core.FileSystem.GenericDataAccess.initWithStringId<State> stateDirPath fileSystem
    let loadState state =
        viewModel.ImageFilePaths <- state.Images
        viewModel.ImageIndex <- state.ImageIndex
    let manager: API.Session.Manager = {
        Load = fun name ->
            match stateDataAccess.TryRead name with
            | Some state -> loadState state
            | None -> ()
        Save = fun name ->
            let state: State = {
                Images = viewModel.ImageFilePaths
                ImageIndex =  viewModel.ImageIndex }
            stateDataAccess.Write name state
        Delete = fun name -> stateDataAccess.EnsureDeleted name
        Reset = fun _ -> loadState State.Default        }
    manager

let initCommandsGroup 
    (viewModel: MainViewModel)
    (getSelectedPath: unit -> Path)
    (getAllPaths: unit -> Path seq)
    commonTags =
    let openSelected = 
        Command.Init "OpenSelected" (fun _ -> 
            match getSelectedPath() with
            | File filePath -> viewModel.SetImages [ filePath ]
            | Directory dirPath -> viewModel.OpenImageSet dirPath
            viewModel.FocusRequest.Trigger() )
    let openAll = 
        Command.Init "OpenAll" (fun _ -> 
            getAllPaths()
            |> Seq.choose (fun path -> match path with File path -> Some path | _ -> None )
            |> viewModel.SetImages
            viewModel.FocusRequest.Trigger() )
    let fitImage = 
        Command.Init "FitImage" (fun _ -> 
            viewModel.FitImage() )
    let commandsGroup: CommandsGroup = {
        Name = "ImageSet"
        Commands = [ openSelected; openAll; fitImage ] }
    commandsGroup
