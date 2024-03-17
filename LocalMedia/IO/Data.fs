module Media.IO.LocalMedia.Data

open Media
open Media.API
open Util.Path
open System

type Config = {
    MediaLocation: DirectoryPath }
with static member Default = {
        MediaLocation = DirectoryPath.None }

module DataAccess =
    type VideoInputServerArguments = {
        command: string seq }

    let init
        (appDataDirPath: DirectoryPath) 
        (temporaryDataDirPath: DirectoryPath)
        (localMediaMetaData: LocalMedia.MetaData)
        (localMediaPreview: LocalMedia.Preview)
        (fileSystem: API.FileSystem)  =
        let trashDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "trash")
        let openVideoFiles = IO.FileSystem.Default.openVideoFiles temporaryDataDirPath
        let openImageFiles = IO.FileSystem.Default.openImageFiles temporaryDataDirPath
        let configDataAccess = 
            let configDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "config")
            let dataAccess = Core.FileSystem.GenericDataAccess.initWithStringId<Config> configDirPath fileSystem
            if dataAccess.List() |> Seq.isEmpty then dataAccess.Write "main" Config.Default
            dataAccess
        let config = configDataAccess.Read "main"
        let dataAccess: LocalMedia.Data = {
            MediaLocation = config.MediaLocation
            OpenOne = fun id ->
                let metaData = localMediaMetaData.FindById id
                match metaData.Path with
                | File path ->
                    if path.Value |> Util.StringMatch.isVideoFile then IO.FileSystem.Default.openVideoFile path
                    elif path.Value |> Util.StringMatch.isImageFile then IO.FileSystem.Default.openImageFile path
                | Directory path ->
                    let files = Util.IO.Directory.listFiles path |> Seq.cache
                    files 
                    |> Seq.filter FilePath.hasVideoExtension
                    |> openVideoFiles
                    files 
                    |> Seq.filter FilePath.hasImageExtension
                    |> Seq.sortBy (fun x -> x.Value)
                    |> openImageFiles
            OpenMany = fun ids ->
                let paths =
                    ids
                    |> Seq.map (fun id -> 
                        let metaData = localMediaMetaData.FindById id
                        metaData.Path )
                    |> Seq.cache
                let filePaths =
                    paths
                    |> Seq.choose (fun path ->
                        match path with
                        | File path -> Some path
                        | _ -> None)
                    |> Seq.cache
                filePaths
                |> Seq.filter FilePath.hasVideoExtension
                |> openVideoFiles
                filePaths
                |> Seq.filter FilePath.hasImageExtension
                |> openImageFiles
            Delete = fun id -> 
                let metaData = localMediaMetaData.FindById id
                match metaData.Path with
                | File path -> 
                    let relativePath = path |> FilePath.toRelativePath
                    let outputTrashFilePath = trashDirPath/relativePath
                    if outputTrashFilePath |> fileSystem.File.Exists then fileSystem.File.Delete outputTrashFilePath
                    if path |> fileSystem.File.Exists then fileSystem.File.Move path outputTrashFilePath
                | Directory path -> 
                    let relativePath = path |> DirectoryPath.toRelativePath
                    let outputTrashDirPath = trashDirPath/relativePath
                    if outputTrashDirPath |> fileSystem.Directory.Exists then fileSystem.Directory.Delete outputTrashDirPath
                    if path |> fileSystem.Directory.Exists then fileSystem.Directory.Move path outputTrashDirPath
                localMediaPreview.Delete metaData.Id
                localMediaMetaData.Delete metaData.Id
            DeleteFile = fun filePath -> 
                let dirName = 
                    filePath 
                    |> FilePath.directoryPath
                    |> DirectoryPath.directoryName
                let trashDirPath = trashDirPath/dirName
                Util.IO.Directory.ensureExists trashDirPath
                if filePath |> Util.IO.File.exists then Util.IO.File.moveToDirectory filePath trashDirPath
            FormatVideo = fun options ->
                let inputFilePath = options.InputFilePath
                let fileName = inputFilePath |> FilePath.fileName
                let tempDirPath =
                    let outputDirName = Guid.NewGuid().ToString() |> DirectoryName
                    temporaryDataDirPath/outputDirName
                let tempOutputFilePath = tempDirPath/fileName
                Util.IO.Media.Video.Format.cutByTimestampRanges inputFilePath options.TimestampRanges tempDirPath tempOutputFilePath
                Util.IO.File.moveToDirectory inputFilePath trashDirPath
                Util.IO.File.move tempOutputFilePath inputFilePath
                Util.IO.Directory.delete tempDirPath
            InitVideoSlideShow = fun videoFilePath ->
                let slideShowId = System.Guid.NewGuid()
                let slideShowTitle = $"slideshow_{slideShowId}"
                let videoInputServer = $"/tmp/{slideShowTitle}"
                let runCommand args =
                    let commandArgs: VideoInputServerArguments = { command = args }
                    let commandArgsString = commandArgs |> Util.Json.toJson
                    use p =
                        new System.Diagnostics.Process()
                        |> Util.Process.useBashScript $"echo '{commandArgsString}' | socat - {videoInputServer}"
                        |> Util.Process.redirectOutput
                    p.Start() |> ignore
                    p.WaitForExit()
                let slideShow: LocalMedia.VideoSlideShow = {
                    Id = slideShowId
                    SeekAbsolute = fun timeInterval -> raise (NotImplementedException())
                    SeekRelative = fun timeInterval -> runCommand ["seek"; timeInterval.TotalSeconds.ToString()]
                    Start = fun _ -> 
                        use p =
                            new System.Diagnostics.Process()
                            |> Util.Process.useBashScript $"mpv --input-ipc-server={videoInputServer} --title={slideShowTitle} --pause '{videoFilePath.Value}' &"
                            |> Util.Process.redirectOutput
                        p.Start() |> ignore
                        p.WaitForExit()
                    Stop = fun _ -> runCommand ["quit"] }
                slideShow
            Import = fun _ -> 
                let allMetaData = localMediaMetaData.ReadAll() |> Seq.cache
                let importVideoFiles (importOptions: LocalMedia.ImportVideoOptions) =
                    Util.IO.Directory.listFilesRecursive importOptions.Source
                    |> Seq.filter (fun filePath -> 
                        filePath |> FilePath.hasVideoExtension && 
                        (allMetaData |> Seq.exists (fun metaData -> metaData.Path = (File filePath) ) |> not)  )
                    |> Seq.iter (fun sourceFilePath -> 
                        let guid = Guid.NewGuid()
                        let title = 
                            let relativePath = 
                                sourceFilePath
                                |> FilePath.relativeTo importOptions.Source
                            let fileName = sourceFilePath |> FilePath.fileNameWithoutExtension
                            if relativePath.Value = (sourceFilePath |> FilePath.fileName |> FileName.value) then fileName.Value
                            else
                                let relativeDirPath = relativePath |> FilePath.directoryPath
                                $"{relativeDirPath.Value}/{fileName.Value}"
                        let filePath =
                            if importOptions.Destination <> DirectoryPath.None then
                                let destinationFilePath =
                                    let extension = sourceFilePath |> FilePath.fileExtension
                                    let newFileName = FileName (guid.ToString()) |> FileName.setExtension extension
                                    importOptions.Destination/newFileName
                                Util.IO.File.move sourceFilePath destinationFilePath
                                destinationFilePath
                            else sourceFilePath
                        let metaData = { MetaData.New guid (File filePath) with Title = title; Tags = ["unwatched"] }
                        localMediaMetaData.Write metaData
                        try localMediaPreview.GeneratePreviews metaData.Id
                        with error -> Diagnostics.exceptWithData error ["Id", metaData.Id])         
                let importImageSets (importOptions: LocalMedia.ImportImageSetsOptions) =
                    Util.IO.Directory.listDirectories importOptions.Source
                    |> Seq.iter (fun imageSetDirPath -> 
                        let guid = System.Guid.NewGuid()
                        let outputDirPath = 
                            let outputDirName = guid.ToString() |> DirectoryName
                            importOptions.Destination/outputDirName
                        Util.IO.Directory.ensureExists outputDirPath
                        Util.IO.Directory.listFilesRecursive imageSetDirPath
                        |> Seq.sortBy FilePath.value
                        |> Seq.iteri(fun i imageFilePath -> 
                            let outputImageFilePath =
                                let extension = imageFilePath |> FilePath.fileName |> FileName.extension
                                let outputImageFileName = 
                                    FileName (sprintf "%03i" i)
                                    |> FileName.setExtension extension
                                outputDirPath/outputImageFileName
                            Util.IO.File.copy imageFilePath outputImageFilePath
                            if importOptions.RemoveFileMetaData then
                                Util.Process.execute $"mat2 --inplace \"{outputImageFilePath.Value}\"" |> ignore)
                        let title = imageSetDirPath |> DirectoryPath.directoryName |> DirectoryName.value
                        let metaData = { MetaData.New guid (Directory outputDirPath) with Title = title; Tags = ["unwatched"] }
                        localMediaMetaData.Write metaData
                        Util.IO.Directory.delete imageSetDirPath )
                let importOptionsSeq =
                    let config = appDataDirPath/DirectoryName "import"
                    let dataAccess = Core.FileSystem.GenericDataAccess.initWithStringId<API.LocalMedia.ImportOptions> config fileSystem
                    dataAccess.ReadAll()
                for importOptions in importOptionsSeq do
                    match importOptions with
                    | LocalMedia.ImportOptions.Video importOptions -> importVideoFiles importOptions
                    | LocalMedia.ImportOptions.ImageSets importOptions -> importImageSets importOptions
        }
        dataAccess
