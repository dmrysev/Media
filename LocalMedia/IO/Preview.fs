module Media.IO.LocalMedia.Preview

open Media
open Media.API
open Util.Path
open System

module DataAccess =
    let readScreenshotsBytes (screenshotsDirPath: DirectoryPath) =
        Util.IO.Directory.listFiles screenshotsDirPath
        |> Seq.map (fun screenshotFilePath -> async {
            return Util.IO.File.readBytes screenshotFilePath })
        |> Async.Parallel 
        |> Async.RunSynchronously
        |> Array.toSeq

    let generateVideoThumbnail (dataFilePath: FilePath) (outputThumbnailFilePath: FilePath) =
        let videoDuration = Util.IO.Media.Video.Info.duration dataFilePath
        let middleDuration = videoDuration / 2.0
        if outputThumbnailFilePath |> Util.IO.File.exists then Util.IO.File.delete outputThumbnailFilePath
        Util.IO.Media.Video.Screenshot.createOne dataFilePath middleDuration outputThumbnailFilePath

    let generateScreenshots (dataFilePath: FilePath) (outputScreenshotsDirPath: DirectoryPath) =
        if outputScreenshotsDirPath |> Util.IO.Directory.exists then Util.IO.Directory.delete outputScreenshotsDirPath
        Util.IO.Directory.create outputScreenshotsDirPath
        Util.IO.Media.Video.Screenshot.createMany dataFilePath 6 outputScreenshotsDirPath

    let init (localMediaMetaData: LocalMedia.MetaData) (appDataDirPath: DirectoryPath) =
        let thumbnailDirPath = appDataDirPath/DirectoryPath "preview/thumbnail"
        let screenshotsDirPath = appDataDirPath/DirectoryPath "preview/screenshots"
        let getThumbnailFilePath (id: Media.Id) = thumbnailDirPath/FileName $"{(Id.String id)}.jpg"
        let getScreenshotsDirPath (id: Media.Id) = screenshotsDirPath/DirectoryName $"{(Id.String id)}"
        let generateThumbnail metaData =
            match metaData.Path with
            | File path ->
                if path |> FilePath.hasVideoExtension then
                    let thumbnailFilePath = getThumbnailFilePath metaData.Id
                    generateVideoThumbnail path thumbnailFilePath
            | _ -> ()
        let hasScreenshotsFiles id =
            getScreenshotsDirPath id
            |> Util.IO.Directory.exists
        let generateScreenshots metaData =
            match metaData.Path with
            | File path ->
                if path |> FilePath.hasVideoExtension then
                    let screenshotsDirPath = getScreenshotsDirPath metaData.Id
                    generateScreenshots path screenshotsDirPath
            | _ -> ()
        let dataAccess: LocalMedia.Preview = {
            GenerateThumbnail = fun id ->
                let metaData = localMediaMetaData.FindById id
                generateThumbnail metaData
            ReadThumbnailBytes = fun id ->
                if getThumbnailFilePath id |> Util.IO.File.exists then
                    getThumbnailFilePath id 
                    |> Util.IO.File.readBytes
                    |> Some
                else
                    let metaData = localMediaMetaData.FindById id
                    match metaData.Path with
                    | File path -> 
                        if path |> FilePath.hasImageExtension then Util.IO.File.readBytes path |> Some
                        else None
                    | Directory path -> 
                        Util.IO.Directory.listFilesRecursive path
                        |> Seq.filter FilePath.hasImageExtension
                        |> Seq.sortBy FilePath.value
                        |> Seq.head
                        |> Util.IO.File.readBytes
                        |> Some
            HasScreenshots = hasScreenshotsFiles
            GenerateScreenshots = fun id ->
                let metaData = localMediaMetaData.FindById id
                generateScreenshots metaData
            GeneratePreviews = fun id -> 
                let metaData = localMediaMetaData.FindById id
                generateThumbnail metaData
                generateScreenshots metaData
            Delete = fun id ->
                getThumbnailFilePath id |> Util.IO.File.delete
                getScreenshotsDirPath id |> Util.IO.Directory.delete
            ReadScreenshotsBytes = fun id ->
                if hasScreenshotsFiles id then
                    getScreenshotsDirPath id
                    |> readScreenshotsBytes
                else
                    let metaData = localMediaMetaData.FindById id
                    match metaData.Path with
                    | File path -> raise (NotImplementedException())
                    | Directory path -> 
                        Util.IO.Directory.listFilesRecursive path
                        |> Seq.filter FilePath.hasImageExtension
                        |> Util.Seq.shuffle
                        |> Seq.take 6
                        |> Seq.sortBy FilePath.value
                        |> Seq.map Util.IO.File.readBytesAsync
                        |> Async.Parallel 
                        |> Async.RunSynchronously
                        |> Array.toSeq
            GetThumbnailLocation = getThumbnailFilePath }
        dataAccess
