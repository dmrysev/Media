namespace Media.Application.MAUI

open Media
open Util.Path
open System

type CoreApplication () =
    let appGuid = "8b63d9a7-1185-4c4c-a4b1-198da456e4a3"
    let appDataDirPath = Util.Environment.SpecialFolder.applicationData
    let dependency, resources = IO.Application.init appDataDirPath appGuid []
    let dependency = {
        dependency with
            Resource = {
                dependency.Resource with
                    ReadImageBytes = fun _ -> Array.empty<byte>  }      }
    member val Dependency = dependency with get

    interface IDisposable with
        member this.Dispose() = 
            resources |> Seq.iter (fun d -> d.Dispose())