module Media.UI.Core.Session

open Media
open Media.API.CommandPrompt
open Util.Path
open System

let initCommandsGroup 
    (appDataDirPath: DirectoryPath) 
    (fileSystem: API.FileSystem)
    (sessionManagers: API.Session.Manager seq)
    sendInfo
    focusInfoOutput
    (finalize: IObservable<unit>) =
    let appDataDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "Session")
    let sessionNamesFilePath = fileSystem.File.Initialize (appDataDirPath/FileName "names.txt")
    let mutable sessionNames = fileSystem.File.ReadAllLines sessionNamesFilePath |> Seq.cache
    let mutable currentSessionName = "none"
    let saveSessionNames() =
        fileSystem.File.WriteLines sessionNamesFilePath sessionNames
    let addSessionName name =
        sessionNames <-
            sessionNames
            |> Util.Seq.appendItem name
            |> Seq.distinct
        saveSessionNames()
    let saveSession name =
        if sessionNames |> Seq.contains name then
            sessionManagers
            |> Seq.iter (fun sessionManager -> sessionManager.Save name)
    finalize.Add (fun _ ->
        saveSession currentSessionName )
    let newSession = { 
        Command.Init "New" (fun options -> 
            saveSession currentSessionName
            let args = options.CommandArguments
            let name = findValue args "Name"
            if sessionNames |> Seq.contains name then
                raise (ArgumentException ($"Session with name '{name}' already exists"))
            addSessionName name
            saveSession name
            currentSessionName <- name )
        with ArgumentDefinitions = [ { 
            CommandArgumentDefinition.Init "Name" with 
                Position = Some 0 } ]     }
    let save =
        Command.Init "Save" (fun options -> 
            if currentSessionName = "none" then 
                raise (ArgumentException ("No active session"))
            saveSession currentSessionName )
    let saveAs = { 
        Command.Init "SaveAs" (fun options -> 
            let args = options.CommandArguments
            let name = findValue args "Name"
            addSessionName name
            saveSession name
            currentSessionName <- name )
        with ArgumentDefinitions = [ { 
            CommandArgumentDefinition.Init "Name" with 
                Position = Some 0
                Suggestions = fun _ -> sessionNames } ]     }
    let load = { 
        Command.Init "Load" (fun options -> 
            saveSession currentSessionName
            let args = options.CommandArguments
            let name = findValue args "Name"
            sessionManagers
            |> Seq.iter (fun sessionManager -> sessionManager.Load name)
            currentSessionName <- name )
        with ArgumentDefinitions = [ { 
            CommandArgumentDefinition.Init "Name" with 
                Position = Some 0
                Suggestions = fun _ -> sessionNames } ] }
    let delete = {
        Command.Init "Delete" (fun options -> 
            let args = options.CommandArguments
            let name = findValue args "Name"
            if sessionNames |> Seq.contains name |> not then
                raise (ArgumentException ($"Session with name '{name}' does not exist"))
            sessionNames <- sessionNames |> Util.Seq.removeItem name
            saveSessionNames()
            sessionManagers
            |> Seq.iter (fun sessionManager -> sessionManager.Delete name)  ) 
        with ArgumentDefinitions = [ { 
            CommandArgumentDefinition.Init "Name" with 
                Position = Some 0
                Suggestions = fun _ -> sessionNames } ]     }
    let reset =
        Command.Init "Reset" (fun options -> 
            if currentSessionName <> "none" then
                saveSession currentSessionName
            currentSessionName <- "none"
            sessionManagers
            |> Seq.iter (fun sessionManager -> sessionManager.Reset()) )
    let showCurrentSessionInfo = 
        Command.Init "ShowCurrentSessionInfo" (fun options -> 
            sendInfo $"Current session name: {currentSessionName}"
            focusInfoOutput() )
    let commandsGroup: CommandsGroup = {
        Name = "Session"
        Commands = [ newSession; save; saveAs; load; delete; reset; showCurrentSessionInfo ] }
    commandsGroup
