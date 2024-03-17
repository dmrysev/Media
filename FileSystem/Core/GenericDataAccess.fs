namespace Media.Core.FileSystem

open Media
open Util.Path

module GenericDataAccess =
    let getFileNameString filePath =
        filePath 
        |> FilePath.fileNameWithoutExtension
        |> FileName.value

    let initWithCustomId<'Id, 'Data when 'Id: equality> 
        (dirPath: DirectoryPath) 
        (fileSystem: API.FileSystem)
        (idToString: 'Id -> string)
        (stringToId: string -> 'Id) =
        fileSystem.Directory.EnsureExists dirPath
        let writeEvent = Event<'Id>()
        let readAll() =
            fileSystem.Directory.ListFiles dirPath
            |> Seq.filter (FilePath.hasExtension "json")
            |> Seq.map (fun filePath -> 
                fileSystem.File.ReadAllText filePath
                |> Util.Json.fromJson<'Data> )
        let filePathIdCompare requestedId filePath =
            let id = 
                getFileNameString filePath
                |> stringToId
            id = requestedId
        let getDataFilePath id =
            fileSystem.Directory.ListFiles dirPath
            |> Seq.filter (FilePath.hasExtension "json")
            |> Seq.find (filePathIdCompare id)
        let dataAccess: API.FileSystem.GenericDataAccess<'Id, 'Data> = {
            List = fun _ ->
                fileSystem.Directory.ListFiles dirPath
                |> Seq.filter (FilePath.hasExtension "json")
                |> Seq.map (fun filePath ->
                    getFileNameString filePath
                    |> stringToId)
            Read = fun id ->
                getDataFilePath id
                |> fileSystem.File.ReadAllText 
                |> Util.Json.fromJson<'Data>
            TryRead = fun id ->
                match
                    fileSystem.Directory.ListFiles dirPath
                    |> Seq.filter (FilePath.hasExtension "json")
                    |> Seq.tryFind (filePathIdCompare id)
                with
                | Some filePath ->
                    filePath
                    |> fileSystem.File.ReadAllText 
                    |> Util.Json.fromJson<'Data>
                    |> Some
                | None -> None
            ReadAll = readAll
            Write = fun id data ->
                let idString = idToString id
                data
                |> Util.Json.toJson
                |> fileSystem.File.WriteText (dirPath/FileName $"{idString}.json")
                writeEvent.Trigger id
            WriteEvent = writeEvent.Publish
            Delete = fun id -> 
                getDataFilePath id
                |> fileSystem.File.Delete
            EnsureDeleted = fun id -> 
                match
                    fileSystem.Directory.ListFiles dirPath
                    |> Seq.filter (FilePath.hasExtension "json")
                    |> Seq.tryFind (filePathIdCompare id)
                with
                | Some filePath -> fileSystem.File.Delete filePath
                | None -> ()   }
        dataAccess

    let initWithGuid<'Data> (dirPath: DirectoryPath) (fileSystem: API.FileSystem) =
        fileSystem.Directory.EnsureExists dirPath
        initWithCustomId<System.Guid, 'Data> dirPath fileSystem (fun id -> id.ToString()) (fun idString -> System.Guid idString)

    let initWithStringId<'Data> (dirPath: DirectoryPath) (fileSystem: API.FileSystem) =
        fileSystem.Directory.EnsureExists dirPath
        initWithCustomId<string, 'Data> dirPath fileSystem (fun id -> id) (fun idString -> idString)
