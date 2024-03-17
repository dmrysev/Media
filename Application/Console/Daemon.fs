module Media.UI.Console.Command.Daemon

open Media
open Media.API
open Util.API
open Util.Path
open CommandLine
open System

[<Verb("daemon", HelpText = "Start daemon.")>]
type Options = { 
    [<Option(Hidden = true)>] PlaceHolder: unit
    
    [<Value(0, MetaName="ServiceName")>] 
    ServiceName: string }

let run (opts: Options) (dep: API.Application.Dependency) =
    Diagnostics.infoMsg "Daemon started"
    let resources = 
        Seq.concat [
            dep.RemoteMedia.InitDownloadQueueDaemon()  ]
        |> Seq.toArray
    System.Console.ReadLine() |> ignore
    