module Media.Core.RemoteMedia

open Media
open Media.API
open Util.Path
open FSharp.Data
open System

let initDownloaderQueueName (downloader: RemoteMedia.IDownloader) =
    $"media/downloader/{downloader.Info.Name}.{downloader.Info.GUID}/download"

let sendInfoEvent message (url: Url) = 
    let details = { Diagnostics.Info.Details.Default with Message = message }
    details.ExtraData["Url"] <- url
    Diagnostics.info details

let download downloadDependencies (localMedia: LocalMedia) (downloader: RemoteMedia.IDownloader) url =
    let task, events = downloader.DownloadAsync downloadDependencies url
    events.Started.Add (sendInfoEvent "Started download")
    events.Finished.Add (sendInfoEvent "Finished download")
    events.Skipped.Add (sendInfoEvent "Skipped download")
    events.Downloaded.Add (fun downloadResult -> 
        let metaData = { downloadResult.MetaData with Tags = downloadResult.MetaData.Tags |> Util.Seq.appendItem "unwatched" }
        localMedia.MetaData.Write metaData
        localMedia.Preview.GeneratePreviews downloadResult.MetaData.Id)
    let result = task |> Async.Catch |> Async.RunSynchronously
    match result with
    | Choice1Of2 _ -> ()
    | Choice2Of2 error -> 
        error.Data["Url"] <- url
        raise error

let update downloadDependencies (localMedia: LocalMedia) (remoteDownloaders: API.RemoteMedia.IDownloader seq) mediaId =
    let metaData = localMedia.MetaData.FindById mediaId
    match remoteDownloaders |> Seq.tryFind (fun x -> x.IsSupportedUrl metaData.Source ) with
    | Some downloader -> 
        let task, events = downloader.DownloadAsync downloadDependencies metaData.Source
        events.Started.Add (sendInfoEvent "Started update")
        events.Finished.Add (sendInfoEvent "Finished update")
        events.Downloaded.Add (fun downloadResult -> 
            let metaData = { 
                downloadResult.MetaData with 
                    Id = metaData.Id
                    Tags = 
                        downloadResult.MetaData.Tags 
                        |> Util.Seq.appendItem "unwatched"
                        |> Seq.distinct }
            localMedia.MetaData.Write metaData)
        let result = task |> Async.Catch |> Async.RunSynchronously
        match result with
        | Choice1Of2 _ -> ()
        | Choice2Of2 error -> 
            error.Data["Url"] <- metaData.Source
            raise error
    | None -> 
        let ex = System.Exception("Not supported url")
        ex.Data["Url"] <- metaData.Source
        raise ex
        