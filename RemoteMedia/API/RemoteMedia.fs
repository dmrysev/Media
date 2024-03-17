namespace Media.API

open Media
open Util.Path

type RemoteMedia = {
    Download: Url -> unit
    InitDownloadQueueDaemon: unit -> System.IDisposable seq
    QueueDownload: Url -> unit
    Update: Media.Id -> unit  }

module RemoteMedia =
    type Info = { Name: string;  GUID: string }

    type IDownloader =
        abstract Info: Info
        abstract IsSupportedUrl: Url -> bool
        abstract DownloadAsync: Dependencies -> Url -> Async<unit> * DownloadEvents
    and Dependencies = {
        MediaDirPath: DirectoryPath
        IsKnownSource: Url -> bool
        GetThumbnailLocation: Media.Id -> FilePath
        GetWebClient: Util.API.Web.Client.ClientType -> Util.API.Web.Client.IWebClient }
    and DownloadEvents = {
        Started: IEvent<Url>
        Finished: IEvent<Url>
        Skipped: IEvent<Url>
        Downloaded: IEvent<DownloadResult> }
    and DownloadResult = { MetaData: MetaData }

    type PublisherDownloadEvents = {
        Started: Event<Url>
        Finished: Event<Url>
        Skipped: Event<Url>
        Downloaded: Event<DownloadResult> }

    type IPreviewsDownloader =
        abstract Info: Info
        abstract DownloadAsync: Dependencies -> PreviewsDownloadConfig -> Async<unit>
        abstract GetFullResolutionUrlForPreviewFile: FilePath -> Url
    and PreviewsDownloadConfig = {
        PreviewsDirPath: DirectoryPath
        AppDataDirPath: DirectoryPath
        Tags: string seq }

    let initPublisherDownloadEvents() =
        let publisherEvents: PublisherDownloadEvents = {
            Started = Event<Url>()
            Finished = Event<Url>()
            Skipped = Event<Url>()
            Downloaded = Event<DownloadResult>() }
        publisherEvents        

    let toSubscriberDownloadEvents publisherEvents =
        let subscriberEvents: DownloadEvents = { 
            Started = publisherEvents.Started.Publish
            Finished = publisherEvents.Finished.Publish
            Skipped = publisherEvents.Skipped.Publish
            Downloaded = publisherEvents.Downloaded.Publish }
        subscriberEvents

    let bindPublisherSubscriberDownloadEvents (publisherEvents: PublisherDownloadEvents) (subscriberEvents: DownloadEvents) =
        subscriberEvents.Started.Add publisherEvents.Started.Trigger
        subscriberEvents.Finished.Add publisherEvents.Finished.Trigger