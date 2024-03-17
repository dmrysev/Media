module Media.IO.RemoteMedia.DownloadQueue

open Media
open Util.Path
open Util.Service.MessageQueueMonitor
open System

type Monitor (download: Url -> unit, config: Config) as this =
    let downloadStateFilePath = Util.Environment.SpecialFolder.temporary/FilePath $"media/download_status/{config.QueueName}"
    let downloadStartedEvent = new Event<Url>()
    let downloadFinishedEvent = new Event<Url>()
    let failedEvent = new Event<ErrorContent>()
    let queueMonitor =
        let messageQueueConfig: Util.Service.MessageQueueMonitor.Config = { QueueName = config.QueueName }
        let monitor = new Util.Service.MessageQueueMonitor.T (messageQueueConfig)
        monitor.NewMessage.Add(fun message -> 
            let url = Url message.Content
            this.SetDownloadState Downloading
            downloadStartedEvent.Trigger url
            let result = download url
            downloadFinishedEvent.Trigger url )
        monitor
        
    member val Events: Events = { 
        DownloadStarted = downloadStartedEvent.Publish
        DownloadFinished = downloadFinishedEvent.Publish
        Failed = failedEvent.Publish  }
    member this.Enqueue (url: Url) = Util.MessageQueue.enqueue config.QueueName url.Value
    member this.EnqueueAsync (url: Url) = Util.MessageQueue.enqueueAsync config.QueueName url.Value
    member this.QueueName = config.QueueName
    member this.GetQueueCount() = Util.MessageQueue.countMessages config.QueueName
    member this.SetDownloadState (state: DownloadState) = Util.Json.toJson state |> Util.IO.File.writeText downloadStateFilePath
    member this.GetDownloadState() = 
        if not (downloadStateFilePath |> Util.IO.File.exists) then Idle
        else Util.IO.File.readAllText downloadStateFilePath |> Util.Json.fromJson<DownloadState>
    member this.ResetQueue() = Util.MessageQueue.resetQueue config.QueueName

    interface System.IDisposable with
        member this.Dispose() =
            (queueMonitor :> IDisposable).Dispose()

and Config = {
    QueueName: string
    UpdateRate: System.TimeSpan
    RequeueOnFail: bool }
with static member Default = {
        QueueName = ""
        UpdateRate = System.TimeSpan.FromSeconds(1.0)
        RequeueOnFail = true }

and Events = {
    DownloadStarted: IEvent<Url>
    DownloadFinished: IEvent<Url>
    Failed: IEvent<ErrorContent> }

and ErrorContent = {
    FailedUrl: Url
    Exception: exn }

and DownloadState = Idle | Downloading | Error
