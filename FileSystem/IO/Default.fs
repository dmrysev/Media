module Media.IO.FileSystem.Default

open Media
open Util.Path

let createPlaylistFile (temporaryDataDirPath: DirectoryPath) (filePaths: FilePath seq) =
    let guid = Util.Guid.generate()
    let playlistFilePath = temporaryDataDirPath/FileName $"{guid}"
    filePaths
    |> Seq.map FilePath.value
    |> Util.IO.File.writeLines playlistFilePath
    playlistFilePath

let openVideoFile (filePath: FilePath) =
    let path = filePath |> Util.IO.File.realPath
    let command = $"mpv --really-quiet --force-window --loop-file --fs '{path.Value}' &"
    Util.Process.executeNoOutput command

let openVideoFiles (temporaryDataDirPath: DirectoryPath) (filePaths: FilePath seq) =
    let playlistFilePath = createPlaylistFile temporaryDataDirPath filePaths
    let command = $"(mpv --really-quiet --force-window --fs --loop-file --playlist='{playlistFilePath.Value}';rm '{playlistFilePath.Value}')&"
    Util.Process.executeNoOutput command

let openImageFile (filePath: FilePath) =
    let command = $"feh -ZFYr '{filePath.Value}' &"
    Util.Process.executeNoOutput command

let openImageFiles (temporaryDataDirPath: DirectoryPath) (filePaths: FilePath seq) =
    let playlistFilePath = createPlaylistFile temporaryDataDirPath filePaths
    let command = $"(feh -ZFYr -f '{playlistFilePath.Value}';rm '{playlistFilePath.Value}')&"
    Util.Process.executeNoOutput command

let openWithSystemDefaultApp (filePath: FilePath) =
    Util.Process.run $"xdg-open '{filePath.Value}'"

type Plugin (pluginsDirPath: DirectoryPath) =
    interface API.FileSystem.IPlugin with
        member this.Load<'a>() = Util.IO.Reflection.loadPlugins<'a> pluginsDirPath

let init (appDataDirPath: DirectoryPath) pluginDirPath =
    let appDataDirPath = Util.IO.Directory.initialize (appDataDirPath/DirectoryName "FileSystem")
    let trashBinDirPath = Util.IO.Directory.initialize (appDataDirPath/DirectoryName "trash")
    let moveFileToTrashBin filePath =
        let relativeFilePath = filePath |> FilePath.toRelativePath
        let outputFilePath = trashBinDirPath/relativeFilePath
        Util.IO.File.move filePath outputFilePath
    let moveDirectoryToTrashBin dirPath =
        let relativeDirPath = dirPath |> DirectoryPath.toRelativePath
        let outputDirPath = trashBinDirPath/relativeDirPath
        Util.IO.Directory.move dirPath outputDirPath
    let dataAccess: API.FileSystem = {
        File = {|
            Initialize = fun filePath ->
                Util.IO.File.ensureExists filePath
                filePath
            ReadAllLines = Util.IO.File.readAllLines
            WriteLines = Util.IO.File.writeLines
            ReadAllText = Util.IO.File.readAllText
            WriteText = Util.IO.File.writeText
            ReadBytes = Util.IO.File.readBytes
            Open = fun filePath ->
                if filePath |> FilePath.hasImageExtension then openImageFile filePath
                elif filePath |> FilePath.hasVideoExtension then openVideoFile filePath
                else openWithSystemDefaultApp filePath
            Exists = Util.IO.File.exists
            EnsureExists = Util.IO.File.ensureExists
            Delete = Util.IO.File.delete
            Move = Util.IO.File.move
            MoveToTrashBin = moveFileToTrashBin
            Copy = Util.IO.File.copy |}
        Directory = {|
            ListFiles = Util.IO.Directory.listFiles
            ListDirectories = Util.IO.Directory.listDirectories
            ListEntries = Util.IO.Directory.listEntries
            Exists = Util.IO.Directory.exists
            EnsureExists = Util.IO.Directory.ensureExists
            Initialize = Util.IO.Directory.initialize
            Delete = Util.IO.Directory.delete
            Move = Util.IO.Directory.move
            MoveToTrashBin = moveDirectoryToTrashBin  |}
        FileSystemEntry = {|
            MoveToTrashBin = fun path ->
                match path with
                | File filePath -> moveFileToTrashBin filePath
                | Directory dirPath -> moveDirectoryToTrashBin dirPath
            Exists = fun path ->
                match path with
                | File filePath -> Util.IO.File.exists filePath
                | Directory dirPath -> Util.IO.Directory.exists dirPath |}
        Clipboard = {|
            GetText = Util.IO.Clipboard.get
            SetText = Util.IO.Clipboard.set |}
        Plugin = Plugin (pluginDirPath) }
    dataAccess
