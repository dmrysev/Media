module Media.IO.RemoteMedia.Default

open Media
open Util.Path

type WebConfig = {
    ProxyIp: string
    ProxyPort: int
    BrowserDebuggerPort: int
    ProxyBrowserDebuggerPort: int
    DriverLocation: DirectoryPath }
with static member Default = {
        ProxyIp = "127.0.0.1"
        ProxyPort = 0
        BrowserDebuggerPort = 0
        ProxyBrowserDebuggerPort = 0
        DriverLocation = DirectoryPath "/usr/bin" }

let init 
    (appDataDirPath: DirectoryPath)
    (fileSystem: API.FileSystem)
    (localMedia: API.LocalMedia) =
    let downloaders = fileSystem.Plugin.Load<API.RemoteMedia.IDownloader>()
    let appDataDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "RemoteMedia")
    let remoteMediaTempDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "tmp")
    let webConfigDataAccess = 
        let configDirPath = appDataDirPath/DirectoryName "web_config"
        let dataAccess = Core.FileSystem.GenericDataAccess.initWithStringId<WebConfig> configDirPath fileSystem
        if dataAccess.List() |> Seq.isEmpty then dataAccess.Write "main" WebConfig.Default
        dataAccess    
    let webConfig = webConfigDataAccess.Read "main"
    let proxySettings: Util.API.Web.Http.Proxy = {
        Ip = webConfig.ProxyIp
        Port = webConfig.ProxyPort }
    let webResources = new Util.Web.Client.Resources(webConfig.DriverLocation, proxySettings, webConfig.BrowserDebuggerPort, webConfig.ProxyBrowserDebuggerPort)    
    let downloadDependencies: API.RemoteMedia.Dependencies = {
        MediaDirPath = localMedia.Data.MediaLocation
        IsKnownSource = localMedia.MetaData.HasSource
        GetThumbnailLocation = localMedia.Preview.GetThumbnailLocation
        GetWebClient = fun clientType -> 
            let client = Util.Web.Client.initWebClient webResources proxySettings remoteMediaTempDirPath clientType
            client.Error.Add (fun details -> API.Diagnostics.exceptWithData details.Error ["Attempt", details.Attempt])
            client }
    let findDownloader url =
        match downloaders |> Seq.tryFind (fun x -> x.IsSupportedUrl url ) with
        | Some downloader -> downloader
        | None -> 
            let ex = System.Exception("Not supported url")
            ex.Data["Url"] <- Url
            raise ex
    let downloadMedia url =
        let downloader = findDownloader url
        Core.RemoteMedia.download downloadDependencies localMedia downloader url
    let dataAccess: API.RemoteMedia = {
        Download = downloadMedia
        InitDownloadQueueDaemon = fun _ ->
            downloaders
            |> Seq.map(fun downloader -> 
                let queueName = Core.RemoteMedia.initDownloaderQueueName downloader
                let download url = 
                    try downloadMedia url
                    with error -> API.Diagnostics.except error
                let config = { IO.RemoteMedia.DownloadQueue.Config.Default with QueueName = queueName }
                new IO.RemoteMedia.DownloadQueue.Monitor (download, config) :> System.IDisposable  )
        QueueDownload = fun url ->
            let downloader = findDownloader url
            let queueName = Core.RemoteMedia.initDownloaderQueueName downloader
            Util.MessageQueue.enqueue queueName url.Value
        Update = Core.RemoteMedia.update downloadDependencies localMedia downloaders  }
    let resources = [
        webResources :> System.IDisposable  ]
    dataAccess, resources
