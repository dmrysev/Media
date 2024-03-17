module Media.IO.Application

open Media
open Util.Path
open System

let init appDataDirPath appGuid argv =
    let diagnosticsDataAccess = IO.Diagnostics.initDataAccess appDataDirPath
    let eventMonitor = new Core.Diagnostics.EventMonitor (appDataDirPath, appGuid, TimeSpan.FromSeconds(5), diagnosticsDataAccess, argv)
    let pluginDirPath = Util.Environment.SpecialFolder.currentAssembly/DirectoryPath "Plugin"
    let fileSystem = IO.FileSystem.Default.init appDataDirPath pluginDirPath
    let fileSystemEnc = IO.FileSystem.Encrypted.init appDataDirPath pluginDirPath
    let localMedia = IO.LocalMedia.Default.init appDataDirPath fileSystem
    let remoteMedia, remoteMediaResources = IO.RemoteMedia.Default.init appDataDirPath fileSystem localMedia
    let dependency: API.Application.Dependency = {
        AppDataDirPath = appDataDirPath
        FileSystem = fileSystem
        FileSystemEnc = fileSystemEnc
        LocalMedia = localMedia
        RemoteMedia = remoteMedia
        Resource = IO.Resource.init()  }
    let resources =
        Seq.concat [ 
            remoteMediaResources
            [ eventMonitor :> IDisposable ] ]
    dependency, resources
