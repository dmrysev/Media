module Media.Test.RemoteMedia

open Media
open Util.Path
open NUnit.Framework
open FsUnit

[<Test>]
let ``If download queue monitor is running, adding url to download queue, must call download and trigger download finished event``() =
    // ARRANGE
    let url = Url "https://somewhere.com/5859634"
    let config: IO.RemoteMedia.DownloadQueue.Config = { 
        QueueName = "media/test/test_download_queue"
        UpdateRate = System.TimeSpan.FromMilliseconds(1.0)
        RequeueOnFail = false }
    let expectedDownloadResult = "downloaded"
    let downloadFake _ = ()
    use monitor = new IO.RemoteMedia.DownloadQueue.Monitor (downloadFake, config)

    // ACT
    Util.MessageQueue.enqueue "media/test/test_download_queue" url.Value
    let downloadedUrl = Async.AwaitEvent monitor.Events.DownloadFinished |> Async.RunSynchronously
    
    // ASSERT
    downloadedUrl |> should equal url


let urlMonitorConfig = {
    IO.RemoteMedia.ClipboardUrlMonitor.Config.UpdateRate = System.TimeSpan.FromMilliseconds(1.0) }

[<Test>]
let ``If clipboard download queue service is running, setting a supported url to clipboard, must trigger added to download queue event``() =
    // ARRANGE
    let task, events = IO.RemoteMedia.ClipboardUrlMonitor.init (fun _ -> true) urlMonitorConfig
    let eventMonitor = Util.Test.EventMonitor(events.SupportedUrlDetected)

    // ACT
    Util.IO.Clipboard.set "https://supported.com/g/1234"
    task |> Async.Start
    Async.AwaitEvent events.ClipboardValueProcessed |> Async.RunSynchronously |> ignore

    // ASSERT
    eventMonitor.TriggerCount |> should equal 1

[<Test>]
let ``If clipboard download queue service is running, setting an unsupported url or other value to clipboard, must not trigger added to download queue event``() =
    // ARRANGE
    let task, events = IO.RemoteMedia.ClipboardUrlMonitor.init (fun _ -> false) urlMonitorConfig
    let eventMonitor = Util.Test.EventMonitor(events.SupportedUrlDetected)
    use cts = new System.Threading.CancellationTokenSource()

    // ACT
    Util.IO.Clipboard.set "https://unsupported.com/g/1234"
    Async.Start(task, cts.Token)
    Async.AwaitEvent events.ClipboardValueProcessed |> Async.RunSynchronously |> ignore

    // ASSERT
    eventMonitor.TriggerCount |> should equal 0
    cts.Cancel()

[<Test>]
let ``If clipboard download queue service is running, setting supported url to clipboard, must clear clipboard``() =
    // ARRANGE
    let task, events = IO.RemoteMedia.ClipboardUrlMonitor.init (fun _ -> true) urlMonitorConfig
    use cts = new System.Threading.CancellationTokenSource()

    // ACT
    Util.IO.Clipboard.set "https://supported.com/g/1234"
    Async.Start(task, cts.Token)
    Async.AwaitEvent events.ClipboardValueProcessed |> Async.RunSynchronously |> ignore

    // ASSERT
    Util.IO.Clipboard.get() |> should equal ""
    cts.Cancel()

[<Test>]
let ``If clipboard download queue service is running, adding unsupported url or other value to clipboard, must not clear clipboard``() =
    // ARRANGE
    let task, events = IO.RemoteMedia.ClipboardUrlMonitor.init (fun _ -> false) urlMonitorConfig
    use cts = new System.Threading.CancellationTokenSource()

    // ACT
    Util.IO.Clipboard.set "https://unsupported.com/g/1234"
    Async.Start(task, cts.Token)
    Async.AwaitEvent events.ClipboardValueProcessed |> Async.RunSynchronously |> ignore

    // ASSERT
    Util.IO.Clipboard.get() |> should equal "https://unsupported.com/g/1234"
    cts.Cancel()
