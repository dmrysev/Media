module Media.UI.Core.ComicBook

open Media
open Media.UI.Core.Command
open Util
open Util.Path
open System

type MainViewModel(
    appDataDirPath: DirectoryPath, 
    getMainViewSize: unit -> Util.Drawing.Size,
    fileSystem: API.FileSystem) =
    inherit UI.Core.ViewModelBase()
    let appDataDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "ComicBook")
    let extractedComicDirPath = appDataDirPath/DirectoryName "extractedComic"
    let comicFilesDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "comics")
    let comicFilePaths =
        fileSystem.Directory.ListFiles comicFilesDirPath
        |> Seq.filter(fun x -> x.Value |> Util.String.toLower |> Util.String.endsWith ".cbz")
        |> Seq.sortBy FilePath.value
    let mutable comicFileIndex = -1
    let mutable imagesDirPath = DirectoryPath.None
    let mutable imageFilePaths = Seq.empty<FilePath>
    let mutable imageIndex = -1
    let newPageImage = Event<byte array>()

    do
        fileSystem.Directory.Delete extractedComicDirPath
        fileSystem.Directory.EnsureExists extractedComicDirPath

    member val ScreenTapped = Event<Util.Drawing.Point>() with get
    member val IsFocused = false with get,set

    member val PageImage = Array.empty<byte> with get,set
    member this.SetPage(index: int) =
        this.PageImage <- 
            imageFilePaths 
            |> Seq.item index
            |> fileSystem.File.ReadBytes
    member this.NextPage() =
        if imageIndex + 1 < (imageFilePaths |> Seq.length) then
            imageIndex <- imageIndex + 1
            this.SetPage(imageIndex)
    member this.PreviousPage() =
        if imageIndex - 1 >= 0 then imageIndex <- imageIndex - 1
        this.SetPage(imageIndex)
    member this.ForwardPageJump(count: int) =
        if imageIndex + count < (imageFilePaths |> Seq.length) then
            imageIndex <- imageIndex + count
            this.SetPage(imageIndex)
    member this.BackwardPageJump(count: int) =
        imageIndex <-
            if imageIndex - count < 0 then 0
            else imageIndex - count
        this.SetPage(imageIndex)

    member val PageImageRotation: double = 0.0 with get,set
    member this.RotateRight = initCommand (fun _ -> this.PageImageRotation <- this.PageImageRotation + 90.0)
    member this.RotateLeft = initCommand (fun  _ -> this.PageImageRotation <- this.PageImageRotation - 90.0)

    member val PageImageScale: double = 1.0 with get,set
    member this.ZoomIn = initCommand (fun _ -> this.PageImageScale <- this.PageImageScale + 0.2)
    member this.ZoomOut = initCommand (fun _ -> this.PageImageScale <- this.PageImageScale - 0.2)

    member val PageImagePositionX: double = 0.0 with get,set
    member val PageImagePositionY: double = 0.0 with get,set
    member this.MoveLeft = initCommand (fun _ -> this.PageImagePositionX <- this.PageImagePositionX - 100.0)
    member this.MoveRight = initCommand (fun _ -> this.PageImagePositionX <- this.PageImagePositionX + 100.0)
    member this.MoveUp = initCommand (fun _ -> this.PageImagePositionY <- this.PageImagePositionY - 100.0)
    member this.MoveDown = initCommand (fun _ -> this.PageImagePositionY <- this.PageImagePositionY + 100.0)

    member val PageImageWidth: double = 100.0 with get,set
    member val PageImageHeight: double = 100.0 with get,set
    member this.FitImage = initCommand (fun _ -> 
        let mainViewSize = getMainViewSize()
        let screenWidth = mainViewSize.Width
        let screenHeight = mainViewSize.Height
        this.PageImageScale <- 1
        this.PageImagePositionX <- 0.0
        this.PageImagePositionY <- 0.0
        this.PageImageWidth <- screenWidth
        this.PageImageHeight <- screenHeight )

    member this.OpenComicFile(comicFilePath: FilePath) =
        fileSystem.Directory.Delete imagesDirPath
        let outputDirPath =
            let outputDirName = 
                comicFilePath 
                |> FilePath.fileNameWithoutExtension
                |> FileName.value
                |> DirectoryName
            extractedComicDirPath/outputDirName
        System.IO.Compression.ZipFile.ExtractToDirectory(comicFilePath.Value, outputDirPath.Value)
        imagesDirPath <- outputDirPath
        imageFilePaths <- 
            fileSystem.Directory.ListFiles outputDirPath
            |> Seq.filter FilePath.hasImageExtension
            |> Seq.sortBy FilePath.value
            |> Seq.cache
        imageIndex <- 0
        this.SetPage(0)
        this.FitImage.Execute()
    member this.SetComicFileIndex(index: int) =
        comicFileIndex <- index
        comicFilePaths
        |> Seq.item comicFileIndex
        |> this.OpenComicFile
    member this.NextComic = initCommand (fun _ ->
        if comicFileIndex + 1 < (comicFilePaths |> Seq.length) then
            comicFileIndex <- comicFileIndex + 1
            this.SetComicFileIndex comicFileIndex )
    member this.PreviousComic = initCommand (fun _ ->
        if comicFileIndex - 1 >= 0 then 
            comicFileIndex <- comicFileIndex - 1
            this.SetComicFileIndex comicFileIndex )

    member val IsMenuVisible = false with get,set

    member this.OnScreenTapped (pos: Util.Drawing.Point) = 
        this.ScreenTapped.Trigger pos
        if this.IsMenuVisible then this.IsMenuVisible <- false
        else 
            let mainViewSize = getMainViewSize()
            let screenWidth = mainViewSize.Width
            let screenHeight = mainViewSize.Height
            if pos.X > screenWidth * 0.8 && pos.Y < screenHeight * 0.2 then 
                this.ForwardPageJump 10
            elif pos.X > screenWidth * 0.2 && pos.X < screenWidth * 0.8 && pos.Y < screenHeight * 0.2 then
                this.IsMenuVisible <- true
            elif pos.X > screenWidth / 2.0 then
                this.NextPage()
            elif pos.X < screenWidth / 2.0 then
                this.PreviousPage()
