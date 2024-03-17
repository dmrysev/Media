module Media.IO.FileSystem.Encrypted

open Media
open Util.Path

let init (appDataDirPath: DirectoryPath) pluginDirPath =
    let appDataDirPath = Util.IO.Directory.initialize (appDataDirPath/DirectoryName "FileSystem")
    let trashBinDirPath = Util.IO.Directory.initialize (appDataDirPath/DirectoryName "trash")
    let keyLocation = appDataDirPath/DirectoryName "key"
    if Util.IO.Directory.exists keyLocation |> not then 
        Util.IO.Directory.create keyLocation
        Util.Encryption.createKeyFile keyLocation
    let key = Util.Encryption.readKeyFile keyLocation
    let defaultDataAccess = IO.FileSystem.Default.init appDataDirPath pluginDirPath
    let dataAccess = { 
        defaultDataAccess with
            File = {| 
                defaultDataAccess.File with
                    ReadBytes = Util.Encryption.decryptFileToBytes key |}     }
    dataAccess
