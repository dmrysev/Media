module Media.UI.Core.LocalMedia.Format

open Media
open Media.API
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive
open System

let initCommandsGroup 
    (getSelectedPath: unit -> Path)
    (localMediaData: LocalMedia.Data) =
    let defaultTimestampRange: Util.Time.Range = {
        Start = TimeSpan.FromSeconds(0)
        End = TimeSpan.FromSeconds(0) }
    let videoFormat = { 
        Command.Init "Video" (fun options ->
            let args = options.CommandArguments
            let options: LocalMedia.FormatVideoOptions = {
                InputFilePath =
                    match getSelectedPath() with
                    | File path -> path
                    | _ -> raise (ArgumentException())
                TimestampRanges = 
                    findValues args "CutOut"
                    |> Seq.map Util.Json.fromJson<Util.Time.Range> }
            localMediaData.FormatVideo options  )
        with 
            ArgumentDefinitions = [{ 
                CommandArgumentDefinition.Init "CutOut" with 
                    Suggestions = fun _ -> [ defaultTimestampRange |> Util.Json.toJson ] }]
            AsyncExecutionEnabled = true  }
    let commandsGroup: CommandsGroup = {
        Name = "Format"
        Commands = [ videoFormat ] }
    commandsGroup
