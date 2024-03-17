module Media.UI.Core.CommandPromptAlias

open Media
open Media.API
open Media.Core
open Media.API.CommandPrompt
open Util.Path
open System

type AliasEntry = { Name: string; Command: string }

let initDataAccess (appDataDirPath: DirectoryPath) (fileSystem: API.FileSystem) =
    let aliasDirPath = appDataDirPath/DirectoryPath "CommandPrompt/aliases"
    Core.FileSystem.GenericDataAccess.initWithStringId<AliasEntry> aliasDirPath fileSystem

type MainViewModel (
    fileSystem: API.FileSystem, 
    appDataDirPath: DirectoryPath) as this =
    inherit UI.Core.ViewModelBase()
    let aliasDataAccess = initDataAccess appDataDirPath fileSystem
    let mutable commandAliasNames = aliasDataAccess.List() |> Seq.cache
    do
        aliasDataAccess.WriteEvent.Add (fun _ -> 
            commandAliasNames <- aliasDataAccess.List() |> Seq.cache
            this.UpdateAutoCompletionItems())

    member val FocusInputEvent = Event<unit>() with get
    member val IsFocused = false with get,set
    member this.Focus() =
        this.IsFocused <- true
        this.FocusInputEvent.Trigger()
        this.SelectedAutoCompletionIndex <- -1

    member this.UpKey() =
        this.AutoCompletionPreviousItem()

    member this.DownKey() =
        this.AutoCompletionNextItem()

    member this.EnterKey() =
        this.AcceptCompletionValue()

    member val NewCommandInput = Event<string>() with get
    member val Input = "" with get,set
    member this.OnInputChanged() = this.UpdateAutoCompletionItems()

    member val AutoCompletionItems = commandAliasNames with get,set
    member this.UpdateAutoCompletionItems() =
        this.AutoCompletionItems <-
            commandAliasNames
            |> Seq.filter (Util.String.contains this.Input)
    member val SelectedAutoCompletionIndex = -1 with get,set
    member this.AcceptCompletionValue() = 
        if this.SelectedAutoCompletionIndex <> -1 then
            let aliasName = this.AutoCompletionItems |> Seq.item this.SelectedAutoCompletionIndex
            let aliasEntry = aliasDataAccess.Read aliasName
            this.NewCommandInput.Trigger aliasEntry.Command
        this.Input <- ""
    member this.AutoCompletionPreviousItem() =
        if (this.AutoCompletionItems |> Seq.length) <> -1 then
            this.SelectedAutoCompletionIndex <-
                if this.SelectedAutoCompletionIndex > 0 then this.SelectedAutoCompletionIndex - 1
                else (this.AutoCompletionItems |> Seq.length) - 1
    member this.AutoCompletionNextItem() =
        if this.SelectedAutoCompletionIndex < (this.AutoCompletionItems |> Seq.length) - 1 then
            this.SelectedAutoCompletionIndex <- this.SelectedAutoCompletionIndex + 1
        elif this.AutoCompletionItems |> Seq.length > 0 then
            this.SelectedAutoCompletionIndex <- 0

let initCommandsGroup 
    (commandsGroups: CommandPrompt.CommandsGroup seq)
    (fileSystem: API.FileSystem)
    (appDataDirPath: DirectoryPath) =
    let aliasDataAccess = initDataAccess appDataDirPath fileSystem
    let mutable commandAliasNames = aliasDataAccess.List() |> Seq.cache
    aliasDataAccess.WriteEvent.Add (fun _ -> 
        commandAliasNames <- aliasDataAccess.List() |> Seq.cache )
    let write = { 
        Command.Init "Write" (fun options -> 
            let args = options.CommandArguments
            let name = findValue args "Name"
            let aliasEntry: AliasEntry = { 
                Name = name
                Command = findValue args "Command" } 
            aliasDataAccess.Write name aliasEntry)
        with ArgumentDefinitions = [
            CommandArgumentDefinition.Init "Name"
            CommandArgumentDefinition.Init "Command" ] }
    let run = {
        Command.Init "Run" (fun options -> 
            let args = options.CommandArguments
            let name = findValue args "Name"
            let aliasEntry = aliasDataAccess.Read name
            aliasEntry.Command
            |> Util.String.split "&&"
            |> Seq.iter (CommandPrompt.executeInputCommand commandsGroups) )
        with 
            ArgumentDefinitions = [
                { CommandArgumentDefinition.Init "Name" with
                    Position = Some 0
                    Suggestions = fun _ -> commandAliasNames } ]
            AsyncExecutionEnabled = true    }
    let commandsGroup: CommandsGroup = {
        Name = "CommandPromptAlias"
        Commands = [ write; run ] }
    commandsGroup
