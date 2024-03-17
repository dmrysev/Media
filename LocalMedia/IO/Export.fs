module Media.IO.LocalMedia.Export

open Media
open Media.API
open Util.Path
open System

module DataAccess =
    let init
        (appDataDirPath: DirectoryPath) 
        (temporaryDataDirPath: DirectoryPath)
        (fileSystem: API.FileSystem) =
        let dataAccess: LocalMedia.Export = {
            ExportToEbook = fun path outputDirPath ->
                match path with
                | Directory path ->
                    let dirName = path |> DirectoryPath.directoryName
                    let temporaryEbookFilePath = temporaryDataDirPath/FileName $"{dirName.Value}.cbz"
                    Util.IO.Media.Ebook.createFromDirectory path temporaryEbookFilePath
                    Util.IO.File.moveToDirectory temporaryEbookFilePath outputDirPath
                | _ -> raise (ArgumentException())
            ExportDirectoryToEbook = fun options ->
                let temporaryDirPath = temporaryDataDirPath/DirectoryName options.OutputName
                Util.IO.Directory.create temporaryDirPath
                Util.IO.Directory.listFilesRecursive options.InputDirectoryPath
                |> Seq.filter FilePath.hasImageExtension
                |> Seq.toArray
                |> fun paths ->
                    if options.Shuffle then Util.Array.shuffle paths
                    else paths
                |> fun paths -> 
                    match options.LimitResult with
                    | Some limit -> Util.Seq.limitItems limit.StartIndex limit.MaxResultCount paths
                    | None -> paths
                |> Seq.iter (fun filePath -> Util.IO.File.copyToDirectory filePath temporaryDirPath)
                let temporaryEbookFilePath = temporaryDataDirPath/FileName $"{options.OutputName}.cbz"
                Util.IO.Media.Ebook.createFromDirectory temporaryDirPath temporaryEbookFilePath
                Util.IO.Directory.delete temporaryDirPath
                Util.IO.File.moveToDirectory temporaryEbookFilePath options.OutputDirectoryPath
            ExportVideoToEbook = fun options -> 
                let inputFilePath = options.InputFilePath
                if inputFilePath |> FilePath.hasVideoExtension |> not then raise (ArgumentException())
                let timeRange: Util.Time.Range = {
                    Start = 
                        match options.TimeRange with
                        | Some range -> range.Start
                        | None -> TimeSpan.FromSeconds (0)
                    End =
                        match options.TimeRange with
                        | Some range -> range.End
                        | None -> Util.IO.Media.Video.Info.duration inputFilePath }
                let outputScreenshotsDirPath = temporaryDataDirPath/DirectoryName options.OutputName
                Util.IO.Directory.create outputScreenshotsDirPath
                Util.Time.splitRangeByInterval timeRange options.TimeInterval
                |> Seq.iteri (fun i time -> 
                    let fileName = sprintf "%03i.jpg" (i + 1) |> FileName
                    let outputScreenshotFilePath = outputScreenshotsDirPath/fileName
                    Util.IO.Media.Video.Screenshot.createOne inputFilePath time outputScreenshotFilePath)
                let temporaryEbookFilePath = temporaryDataDirPath/FileName $"{options.OutputName}.cbz"
                Util.IO.Media.Ebook.createFromDirectory outputScreenshotsDirPath temporaryEbookFilePath
                Util.IO.Directory.delete outputScreenshotsDirPath
                Util.IO.File.moveToDirectory temporaryEbookFilePath options.OutputDirectoryPath
            ExportToMobileDevice = fun path outputDirPath ->
                Util.IO.MobileDevice.send path outputDirPath 
            ReadCommonOutputLocations = fun _ -> 
                fileSystem.File.Initialize (appDataDirPath/FileName "common_export_output_locations")
                |> fileSystem.File.ReadAllLines
                |> Seq.map DirectoryPath                
        }
        dataAccess
