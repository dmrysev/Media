module Media.Core.Diagnostics

open Media
open Media.API
open Util.Path
open System

let addErrorDetails (processInstanceDetailsId: Diagnostics.ProcessInstance.Id) (details: Diagnostics.Error.Details) = { 
    details with 
        EventId = if details.EventId <> "" then details.EventId else Guid.NewGuid().ToString()
        DateTimeUtc = DateTime.UtcNow
        DateTimeLocal = DateTime.Now
        ProcessInstanceId = processInstanceDetailsId }

let addInfoDetails (processInstanceDetailsId: Diagnostics.ProcessInstance.Id) (details: Diagnostics.Info.Details) = { 
    details with 
        EventId = if details.EventId <> "" then details.EventId else Guid.NewGuid().ToString()
        DateTimeUtc = DateTime.UtcNow
        DateTimeLocal = DateTime.Now
        ProcessInstanceId = processInstanceDetailsId }

let formatErrorDetailsLog (details: Diagnostics.Error.Details) =
    let dateTime = details.DateTimeLocal |> Util.Json.toJson
    let firstMediaOccuranceInStackTrace =
        let findResult =
            details.Exception.StackTrace 
            |> Util.String.split "\n"
            |> Seq.tryFind (fun s -> s |> Util.String.contains "at Media.")
        match findResult with
        | Some stack -> stack
        | None -> ""
    let extraData = details.ExtraData |> Util.Json.toJson
    let exceptionData = details.Exception.Data |> Util.Json.toJson
    let severity = details.Severity.ToString() |> Util.String.toUpper
    sprintf $"{dateTime} [ERROR] [{severity}] {details.Message} {extraData} {details.Exception.Message} {firstMediaOccuranceInStackTrace} {exceptionData} EventId = {details.EventId}\n{details.Exception.StackTrace}"

let formatInfoDetailsLog (details: Diagnostics.Info.Details) =
    let dateTime = details.DateTimeLocal |> Util.Json.toJson
    let extraData = details.ExtraData |> Util.Json.toJson
    let verbosity = details.Verbosity.ToString() |> Util.String.toUpper
    sprintf $"{dateTime} [{verbosity}] {details.Message} {extraData} EventId = {details.EventId}"

type EventMonitor (
    appDataDirPath: DirectoryPath, 
    applicationGuid: string,
    flushTimeout: System.TimeSpan,
    diagnosticsDataAccess: Diagnostics.DataAccess,
    ?applicationArguments: string seq) =
    let errorQueue = Collections.Concurrent.ConcurrentQueue<Diagnostics.Error.Details>()
    let infoQueue = Collections.Concurrent.ConcurrentQueue<Diagnostics.Info.Details>()
    let processInstanceDetails = {
        Diagnostics.ProcessInstance.Details.Default with
            Id = Diagnostics.ProcessInstance.generateId()
            // Application = Reflection.Assembly.GetEntryAssembly().Location |> FilePath
            // ApplicationRevision = readRevision()
            ApplicationGuid = applicationGuid
            ApplicationData = appDataDirPath
            // ApplicationDataRevision = Util.VersionControl.currentRevision appDataDirPath 
            ProcessId = Environment.ProcessId
            Arguments = defaultArg applicationArguments Seq.empty
        }
    let flush() =
        try 
            while errorQueue.IsEmpty |> not do
                let isSuccess, details = errorQueue.TryDequeue()
                if isSuccess then 
                    try 
                        diagnosticsDataAccess.WriteErrorEvent details
                        details |> formatErrorDetailsLog |> (printfn "%s")
                    with error -> printfn $"{error} {details}"
                else failwith "Failed to dequeue"
            while infoQueue.IsEmpty |> not do
                let isSuccess, details = infoQueue.TryDequeue()
                if isSuccess then 
                    try 
                        diagnosticsDataAccess.WriteInfoEvent details
                        details |> formatInfoDetailsLog |> (printfn "%s")
                    with error -> printfn $"{error} {details}"
                else failwith "Failed to dequeue"
        with error -> printfn $"{error}"
    let flushTimer = 
        let timer = new Timers.Timer (flushTimeout)
        timer.AutoReset <- true
        timer.Elapsed.Add(fun _ -> flush())
        timer
    do
        diagnosticsDataAccess.WriteProcessDetails processInstanceDetails
        Diagnostics.Error.subscriber.Add (fun details -> details |> addErrorDetails processInstanceDetails.Id |> errorQueue.Enqueue)
        Diagnostics.Info.subscriber.Add (fun details -> details |> addInfoDetails processInstanceDetails.Id |> infoQueue.Enqueue)
        flushTimer.Start()
    interface System.IDisposable with
        member this.Dispose() = 
            flushTimer.Stop()
            flushTimer.Dispose()
            flush()
