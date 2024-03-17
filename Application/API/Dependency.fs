namespace Media.API.Application

open Media
open Util
open Util.Path

type Dependency = {
    AppDataDirPath: DirectoryPath
    FileSystem: API.FileSystem
    FileSystemEnc: API.FileSystem
    LocalMedia: API.LocalMedia
    RemoteMedia: API.RemoteMedia
    Resource: API.Resource  }
