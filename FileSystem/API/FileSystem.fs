namespace Media.API

open Util.Path

module FileSystem =
    type GenericDataAccess<'Id, 'Data when 'Id: equality> = {
        List: unit -> 'Id seq
        Read: 'Id -> 'Data
        TryRead: 'Id -> 'Data option
        ReadAll: unit -> 'Data seq
        Write: 'Id -> 'Data -> unit
        WriteEvent: IEvent<'Id>
        Delete: 'Id -> unit
        EnsureDeleted: 'Id -> unit }
        
    type IPlugin =
        abstract Load<'a> : unit -> 'a seq

type FileSystem = {
    File: {|
        Initialize: FilePath -> FilePath
        ReadAllLines: FilePath -> string array
        WriteLines: FilePath -> string seq -> unit
        ReadAllText: FilePath -> string
        WriteText: FilePath -> string -> unit
        ReadBytes: FilePath -> byte array
        Open: FilePath -> unit
        Exists: FilePath -> bool
        EnsureExists: FilePath -> unit
        Delete: FilePath -> unit
        Move: FilePath -> FilePath -> unit
        MoveToTrashBin: FilePath -> unit
        Copy: FilePath -> FilePath -> unit |}
    Directory: {|
        ListFiles: DirectoryPath -> FilePath seq
        ListDirectories: DirectoryPath -> DirectoryPath seq
        ListEntries: DirectoryPath -> Path seq
        Exists: DirectoryPath -> bool
        EnsureExists: DirectoryPath -> unit
        Initialize: DirectoryPath -> DirectoryPath
        Delete: DirectoryPath -> unit
        Move: DirectoryPath -> DirectoryPath -> unit
        MoveToTrashBin: DirectoryPath -> unit |}
    FileSystemEntry: {|
        MoveToTrashBin: Path -> unit
        Exists: Path -> bool |}
    Clipboard: {|
        GetText: unit -> string
        SetText: string -> unit |}
    Plugin: FileSystem.IPlugin   }
