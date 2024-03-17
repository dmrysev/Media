module Media.IO.Diagnostics

open Media
open Media.API
open Util.Path
open System

let initDataAccess (appDataDirPath: DirectoryPath) =
    let logDataDirPath = 
        let dirPath = appDataDirPath/DirectoryPath "Diagnostics/log"
        Util.IO.Directory.ensureExists dirPath
        dirPath
    let processDetailsDataAccess =
        let processDetailsDirPath = logDataDirPath/DirectoryName "process_details"
        Util.IO.Directory.ensureExists processDetailsDirPath
        Util.DataAccess.JsonFileDataAccess (processDetailsDirPath)
    let errorDataAccess = 
        let errorLogDataDirPath = logDataDirPath/DirectoryName "error"
        Util.IO.Directory.ensureExists errorLogDataDirPath
        Util.DataAccess.JsonFileDataAccess (errorLogDataDirPath)
    let infoDataAccess = 
        let infoLogDataDirPath = logDataDirPath/DirectoryName "info"
        Util.IO.Directory.ensureExists infoLogDataDirPath
        Util.DataAccess.JsonFileDataAccess (infoLogDataDirPath)
    let dataAccess: Diagnostics.DataAccess = {
        WriteProcessDetails = fun details ->
            details
            |> Util.Json.toJson
            |> processDetailsDataAccess.Write details.Id.Value
        FindProcessDetailsById = fun id -> 
            processDetailsDataAccess.FindById id.Value
            |> Util.Json.fromJson<Diagnostics.ProcessInstance.Details>
        WriteErrorEvent = fun details -> 
            details
            |> Util.Json.toJson
            |> errorDataAccess.Write details.EventId
        ReadErrorEvents = fun _ -> 
            errorDataAccess.ReadAll()
            |> Seq.map Util.Json.fromJson<Diagnostics.Error.Details>
        WriteInfoEvent = fun details ->
            details
            |> Util.Json.toJson
            |> infoDataAccess.Write details.EventId
        ReadInfoEvents = fun _ ->
            infoDataAccess.ReadAll()
            |> Seq.map Util.Json.fromJson<Diagnostics.Info.Details>}
    dataAccess

let revisionFileName = FileName "revision.txt"

let readRevision() =
    let assemblyFilePath = 
        Reflection.Assembly.GetEntryAssembly().Location 
        |> FilePath
        |> FilePath.directoryPath
    let revisionFilePath = assemblyFilePath/revisionFileName
    Util.IO.File.readAllLines revisionFilePath |> Seq.head
