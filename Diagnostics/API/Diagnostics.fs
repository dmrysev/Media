module Media.API.Diagnostics

open Util.Path
open System

module ProcessInstance =
    type Id = { Value: string }
    with static member None = { Value = "none" }

    let id value = { Value = value }
    let generateId() = id (Util.Guid.generate())

    type Details = {
        Id: Id
        Application: FilePath
        ApplicationGuid: string
        ApplicationRevision: string
        ApplicationData: DirectoryPath
        ApplicationDataRevision: string 
        Arguments: string seq
        ProcessId: int}
    with static member Default = {
            Id = generateId()
            Application = FilePath.None
            ApplicationGuid = ""
            ApplicationRevision = ""
            ApplicationData = DirectoryPath.None
            ApplicationDataRevision = "" 
            Arguments = Seq.empty
            ProcessId = -1}

module Error =
    type Details = {
        ErrorId: string
        EventId: string
        Severity: Severity
        Message: string
        Exception: Exception
        DateTimeUtc: DateTime
        DateTimeLocal: DateTime
        IsResolved: bool
        ExtraInfo: string
        ExtraData: Collections.IDictionary
        ProcessInstanceId: ProcessInstance.Id }
    with static member Default = {
            ErrorId = ""
            EventId = ""
            Severity = Mild
            Message = ""
            Exception = System.Exception()
            DateTimeUtc = DateTime.UtcNow
            DateTimeLocal = DateTime.Now
            IsResolved = false
            ExtraInfo = ""
            ExtraData = Collections.Generic.Dictionary<string,obj>()
            ProcessInstanceId = ProcessInstance.Id.None }
    and Severity = Mild | Important | Critical

    let event = Event<Details>()
    let send = event.Trigger
    let subscriber = event.Publish

let error = Error.send
let except e = Error.send { Error.Details.Default with Exception = e }
let exceptWithData (ex: Exception) (data: seq<string * obj>) = 
    for key, value in data do ex.Data[key] <- value
    Error.send { Error.Details.Default with Exception = ex }

module Info =
    type Details = {
        InfoId: string
        EventId: string
        Message: string
        Verbosity: Verbosity
        DateTimeUtc: DateTime
        DateTimeLocal: DateTime
        StackTrace: string
        ExtraInfo: string
        ExtraData: Collections.IDictionary
        ProcessInstanceId: ProcessInstance.Id }
    with static member Default = {
            InfoId = ""
            EventId = ""
            Message = ""
            Verbosity = Info
            DateTimeUtc = DateTime.UtcNow
            DateTimeLocal = DateTime.Now
            StackTrace = ""
            ExtraInfo = ""
            ExtraData = Collections.Generic.Dictionary<string,obj>()
            ProcessInstanceId = ProcessInstance.Id.None }
    and Verbosity = Debug | Info | Warning

    let event = Event<Details>()
    let send details = 
        let stackTrace = details.StackTrace |> Util.String.defaultIfEmpty (Diagnostics.StackTrace(true).ToString())
        event.Trigger { details with StackTrace = stackTrace }
    let subscriber = event.Publish

let info = Info.send
let infoMsg msg = Info.send { Info.Details.Default with Message = msg }
let infoWithData (msg: string) (data: seq<string * obj>) = 
    let details = { Info.Details.Default with Message = msg }
    for key, value in data do details.ExtraData[key] <- value
    Info.send details

type DataAccess = {
    WriteProcessDetails: ProcessInstance.Details -> unit
    FindProcessDetailsById: ProcessInstance.Id ->ProcessInstance.Details
    WriteErrorEvent: Error.Details -> unit
    ReadErrorEvents: unit ->Error.Details seq
    WriteInfoEvent: Info.Details -> unit
    ReadInfoEvents: unit ->Info.Details seq  }
