#nowarn "0058"

open Media
open Media.UI.Console
open Media.API
open Util.API
open Util.Path
open CommandLine
open System

[<EntryPoint>]
let main argv =
    let appGuid = "3610fe17-3b3d-45c4-af18-4ca4d59f22f4"
    let result = CommandLine.Parser.Default.ParseArguments<
        Command.Download.Options,
        Command.Update.Options,
        Command.Import.Options, 
        Command.Open.Options,
        Command.Daemon.Options>(argv)
    match result with
    | :? CommandLine.Parsed<obj> as command ->
        let appDataDirPath = Util.Environment.SpecialFolder.applicationData/DirectoryName "media"
        let dependency, resources = IO.Application.init appDataDirPath appGuid argv
        try 
            match command.Value with
            | :? Command.Download.Options as options -> Command.Download.run options dependency
            | :? Command.Update.Options as options -> Command.Update.run options dependency
            | :? Command.Import.Options as options -> Command.Import.run options dependency
            | :? Command.Open.Options as options -> Command.Open.run options dependency
            | :? Command.Daemon.Options as options -> Command.Daemon.run options dependency
            | _ -> ()
        with error -> 
            Diagnostics.error { 
                Diagnostics.Error.Details.Default with 
                    Message = "Failed in main"
                    Exception = error
                    Severity = Diagnostics.Error.Severity.Critical }
            resources |> Seq.iter (fun d -> d.Dispose())
            raise error
        resources |> Seq.iter (fun d -> d.Dispose())
    | _ -> ()
    0