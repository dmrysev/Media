namespace Media.UI.Core.LocalMedia

open Media
open Media.API
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive
open System

module Export =
    let initCommandsGroup 
        (getSelectedPath: unit -> Path)
        (localMediaExport: LocalMedia.Export)
        (commonLocations: DirectoryPath seq)
        sendInfo =
        let commonLocations = 
            commonLocations
            |> Seq.map DirectoryPath.value
            |> Seq.cache
        let commonExportOutputLocations = 
            localMediaExport.ReadCommonOutputLocations()
            |> Seq.map DirectoryPath.value
            |> Seq.cache
        let blankLimitOptions: LocalMedia.LimitResultOptions = { StartIndex = 0; MaxResultCount = 0 }
        let ebook = 
            { Command.Init "Ebook" (fun options -> 
                let args = options.CommandArguments
                let outputDirPath = findValue args "OutputLocation" |> DirectoryPath
                let path = getSelectedPath()
                localMediaExport.ExportToEbook path outputDirPath )
              with 
                ArgumentDefinitions = [ 
                    { CommandArgumentDefinition.Init "OutputLocation" with 
                        Position = Some 0
                        Suggestions = fun _ -> commonExportOutputLocations } ] 
                AsyncExecutionEnabled = true        }
        let dirToEbook = 
            { Command.Init "DirectoyToEbook" (fun options -> 
                let args = options.CommandArguments
                let exportOptions: LocalMedia.ExportDirectoryToEbookOptions = { 
                    InputDirectoryPath = findValue args "InputDirectory" |> DirectoryPath
                    OutputDirectoryPath = findValue args "OutputLocation" |> DirectoryPath
                    OutputName = findValue args "OutputName"
                    Shuffle = isFlagSet args "Shuffle"
                    LimitResult = tryFindRecordValue<LocalMedia.LimitResultOptions> args "Limit" }
                localMediaExport.ExportDirectoryToEbook exportOptions )
              with 
                ArgumentDefinitions = [ 
                    { CommandArgumentDefinition.Init "InputDirectory" with 
                        Suggestions = fun _ -> commonLocations }
                    { CommandArgumentDefinition.Init "OutputLocation" with 
                        Suggestions = fun _ -> commonExportOutputLocations }
                    CommandArgumentDefinition.Init "OutputName"
                    CommandArgumentDefinition.Init "Shuffle"
                    { CommandArgumentDefinition.Init "Limit" with 
                        Suggestions = fun _ -> [ blankLimitOptions ] |> Seq.map Util.Json.toJson } ]
                AsyncExecutionEnabled = true         }
        let videoToEbook = 
            { Command.Init "VideoToEbook" (fun options -> 
                let args = options.CommandArguments
                let exportOptions: LocalMedia.ExportVideoToEbookOptions = {
                    InputFilePath = 
                        match getSelectedPath() with
                        | File path -> path
                        | _ -> raise (ArgumentException())
                    OutputDirectoryPath = findValue args "OutputLocation" |> DirectoryPath
                    OutputName = findValue args "OutputName"
                    TimeInterval = findRecordValue<TimeSpan> args "TimeInterval"
                    TimeRange = findRecordValue<Util.Time.Range> args "TimeRange" |> Some }
                localMediaExport.ExportVideoToEbook exportOptions )
              with 
                ArgumentDefinitions = [ 
                    { CommandArgumentDefinition.Init "OutputLocation" with 
                        Suggestions = fun _ -> commonExportOutputLocations }
                    CommandArgumentDefinition.Init "OutputName"
                    { CommandArgumentDefinition.Init "TimeInterval" with 
                        Suggestions = fun _ -> [ 
                            TimeSpan.FromSeconds(1) |> Util.Json.toJson
                            TimeSpan.FromSeconds(2) |> Util.Json.toJson
                            TimeSpan.FromSeconds(5) |> Util.Json.toJson
                            TimeSpan.FromSeconds(10) |> Util.Json.toJson ]  } 
                    { CommandArgumentDefinition.Init "TimeRange" with 
                        Suggestions = fun _ ->
                            let defaultTimeRange: Util.Time.Range = { 
                                Start = TimeSpan.FromSeconds(0)
                                End = TimeSpan.FromMinutes(1) }
                            [ defaultTimeRange |> Util.Json.toJson ] } ]
                AsyncExecutionEnabled = true   }
        let sendToMobileDevice = 
            { Command.Init "SendToMobileDevice" (fun options -> 
                let args = options.CommandArguments
                let outputDirPath = findValue args "OutputLocation" |> DirectoryPath
                let path = getSelectedPath()
                localMediaExport.ExportToMobileDevice path outputDirPath ) 
              with 
                ArgumentDefinitions = [ 
                    { CommandArgumentDefinition.Init "OutputLocation" with 
                        Position = Some 0
                        Suggestions = fun _ -> commonExportOutputLocations } ]
                AsyncExecutionEnabled = true       }
        let commandsGroup: CommandsGroup = {
            Name = "Export"
            Commands = [ ebook; dirToEbook; videoToEbook; sendToMobileDevice ] }
        commandsGroup