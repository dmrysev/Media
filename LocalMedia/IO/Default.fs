module Media.IO.LocalMedia.Default

open Media
open Media.API
open Util.Path
open System

let init (appDataDirPath: DirectoryPath) (fileSystem: API.FileSystem) =
    let appDataDirPath = appDataDirPath/DirectoryName "LocalMedia"
    let temporaryDataDirPath = appDataDirPath/DirectoryName "tmp"
    [ appDataDirPath; temporaryDataDirPath ] |> Seq.iter Util.IO.Directory.ensureExists
    let localMediaMetaData = IO.LocalMedia.MetaData.DataAccess.init appDataDirPath
    let localMediaPreview = IO.LocalMedia.Preview.DataAccess.init localMediaMetaData appDataDirPath
    let localMediaData = IO.LocalMedia.Data.DataAccess.init appDataDirPath temporaryDataDirPath localMediaMetaData localMediaPreview fileSystem
    let dataAccess: LocalMedia = {
        Data = localMediaData
        Export = IO.LocalMedia.Export.DataAccess.init appDataDirPath temporaryDataDirPath fileSystem
        MetaData = localMediaMetaData
        Preview = localMediaPreview }
    dataAccess   
