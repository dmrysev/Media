module Media.Test.Diagnostics

open Media
open Media.API.Diagnostics
open Media.TestUtil
open Util.Path
open NUnit.Framework
open FsUnit

let outputDirPath = generateTemporaryFolder()/DirectoryName "diagnostics"
let initDataAccess() = IO.Diagnostics.initDataAccess (outputDirPath/DirectoryName "log")

[<SetUp>]
let setUp () = 
    Util.IO.Directory.delete outputDirPath

[<TearDown>]
let tearDown () = 
    Util.IO.Directory.delete outputDirPath

[<Test>]
let ``Writing process details and then finding it by id, must return same details``() =
    // ARRANGE
    let dataAccess = initDataAccess()
    let id = ProcessInstance.id "test_process_details_id"
    let details = { 
        ProcessInstance.Details.Default with 
            Id = id
            Arguments = [ "arg1"; "arg2" ] }

    // ACT
    dataAccess.WriteProcessDetails details
    let result = dataAccess.FindProcessDetailsById id

    // ASSERT
    result |> should equal details

[<Test>]
let ``Writing error event details and then reading it back, must return same details``() =
    // ARRANGE
    let dataAccess = initDataAccess()

    // ACT
    dataAccess.WriteErrorEvent { Error.Details.Default with EventId = "event_id_1" }
    dataAccess.WriteErrorEvent { Error.Details.Default with EventId = "event_id_2" }
    let result = dataAccess.ReadErrorEvents() |> Seq.map (fun x -> x.EventId)

    // ASSERT
    result |> should equivalent [ "event_id_1"; "event_id_2" ]

[<Test>]
let ``Writing info event details and then reading it back, must return same details``() =
    // ARRANGE
    let dataAccess = initDataAccess()

    // ACT
    dataAccess.WriteInfoEvent { Info.Details.Default with EventId = "event_id_1" }
    dataAccess.WriteInfoEvent { Info.Details.Default with EventId = "event_id_2" }
    let result = dataAccess.ReadInfoEvents() |> Seq.map (fun x -> x.EventId)

    // ASSERT
    result |> should equivalent [ "event_id_1"; "event_id_2" ]
