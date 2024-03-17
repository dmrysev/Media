module Media.IO.RemoteMedia.ClipboardUrlMonitor

open Util
open Util.Path

type Events = {
    ClipboardValueProcessed: IEvent<string>
    SupportedUrlDetected: IEvent<Url> }

type Config = {
    UpdateRate: System.TimeSpan }

let initDaemon (timeout: System.TimeSpan) func = async {
    while true do
        do! func()
        do! Util.Async.sleep timeout }

let init (isSupportedUrl: Url -> bool) (config: Config) =
    let clipboardValueProcessedEvent = new Event<string>()
    let supportedUrlDetectedEvent = new Event<Url>()
    let events = {
        ClipboardValueProcessed = clipboardValueProcessedEvent.Publish
        SupportedUrlDetected = supportedUrlDetectedEvent.Publish }
    let task = initDaemon config.UpdateRate (fun _ -> async {
        let clipboardValue = Util.IO.Clipboard.get()
        if clipboardValue |> StringMatch.isUrl && clipboardValue |> Url |> isSupportedUrl then
            let url = Url clipboardValue
            supportedUrlDetectedEvent.Trigger url
            Util.IO.Clipboard.clear()
        clipboardValueProcessedEvent.Trigger clipboardValue } )
    (task, events)
