module Media.UI.Core.CommandPrompt

open Media
open Media.API
open Media.API.CommandPrompt
open Media.Core.CommandPrompt
open Util.Path

type Focus = CommandInput | CommandHistory | AutoCompletion

type MainViewModel (
    commandsGroups: CommandsGroup seq, 
    fileSystem: API.FileSystem,
    appDataDirPath: DirectoryPath,
    getMainViewSize: unit -> Util.Drawing.Size,
    sendInfo) =
    inherit UI.Core.ViewModelBase()

    let appDataDirPath = fileSystem.Directory.Initialize (appDataDirPath/DirectoryName "CommandPrompt")

    let initAutoCompletionItems = initAutoCompletionItems commandsGroups
    let parseCommandArguments = parseCommandArguments commandsGroups
    let acceptCompletionValue = acceptCompletionValue commandsGroups
    let mutable inputIndexes = parseInputIndexes ""
    
    let commandHistoryFilePath = fileSystem.File.Initialize (appDataDirPath/FileName "command_history")
    let readCommandHistory() = 
        fileSystem.File.ReadAllLines commandHistoryFilePath
        |> Array.toSeq
    let mutable commandHistory = readCommandHistory()
    let addCommandHistory command =
        commandHistory <-
            if commandHistory |> Seq.contains command then
                commandHistory |> Util.Seq.removeItem command
            else commandHistory
            |> Util.Seq.appendItem command
        fileSystem.File.WriteLines commandHistoryFilePath commandHistory

    member val Width = 100.0 with get,set

    member val FocusInputEvent = Event<unit>()  with get
    member val IsFocused = false with get,set
    member this.Focus() =
        let mainViewSize = getMainViewSize()
        this.Width <- mainViewSize.Width
        this.IsFocused <- true
        this.SetFocus Focus.CommandInput
    member val CurrentFocus = Focus.CommandInput with get,set
    member this.UpdateView() =
        this.IsCommandHistoryVisible <- false
        match this.CurrentFocus with
        | CommandInput -> this.FocusInputEvent.Trigger()
        | CommandHistory -> 
            this.IsAutoCompletionVisible <- false
            this.FocusCommandHistory()
        | AutoCompletion -> this.FocusAutoCompletion()
    member this.SetFocus (focus: Focus) =
        this.CurrentFocus <- focus
        this.UpdateView()

    member this.UpKey() =
        match this.CurrentFocus with
        | CommandInput -> this.SetFocus Focus.CommandHistory
        | CommandHistory -> this.CommandHistoryPreviousItem()
        | AutoCompletion -> this.AutoCompletionPreviousItem()

    member this.DownKey() =
        match this.CurrentFocus with
        | CommandInput -> this.SetFocus Focus.AutoCompletion
        | CommandHistory -> this.CommandHistoryNextItem()
        | AutoCompletion -> this.AutoCompletionNextItem()

    member this.EnterKey() =
        match this.CurrentFocus with
        | CommandInput -> this.Run()
        | CommandHistory -> 
            this.AcceptCommandHistoryValue()
            this.SetFocus Focus.CommandInput
        | AutoCompletion -> 
            this.AcceptCompletionValue()
            this.SetFocus Focus.CommandInput

    member val Input = "" with get,set
    member this.OnInputChanged() =
        inputIndexes <- parseInputIndexes this.Input
        this.IsAutoCompletionVisible <- this.IsInputFocused && this.Input <> ""
        this.UpdateCommandHistory()
    member this.SetInput command =
        this.Input <- command
        this.SeekCaretIndexToEnd()
    
    member val IsInputFocused = true with get,set
    member this.OnIsInputFocusedChanged() =
        this.IsAutoCompletionVisible <- this.IsInputFocused && this.Input <> ""

    member val CaretIndex = -1 with get,set
    member this.OnCaretIndexChanged () = 
        let inputArguments = parseCommandArguments this.Input inputIndexes
        let suggestionsArguments: SuggestionsArguments = { InputArguments = inputArguments }
        let inputState = determineInputState this.Input inputIndexes this.CaretIndex
        this.AutoCompletionItems <- initAutoCompletionItems suggestionsArguments this.Input inputState
    member this.SeekCaretIndexToEnd() = this.CaretIndex <- this.Input.Length
    member val CursorPosition = 0 with get,set
    member this.OnCursorPositionChanged() = this.CaretIndex <- this.CursorPosition

    member val AutoCompletionItems = Seq.empty<string> with get,set
    member this.OnAutoCompletionItemsChanged() = this.SetSelectedAutoCompletionIndex -1
    member val SelectedAutoCompletionIndex = -1 with get,set
        
    member this.SetSelectedAutoCompletionIndex (index: int) =
        if index = -1 || this.AutoCompletionItems |> Seq.isEmpty then
            this.SelectedAutoCompletionIndex <- -1
        elif index <= (this.AutoCompletionItems |> Util.Seq.lastIndex) then
            this.SelectedAutoCompletionIndex <- index
    member val IsAutoCompletionVisible = false with get,set
    member this.AcceptCompletionValue() = 
        if this.SelectedAutoCompletionIndex <> -1 then
            let inputState = determineInputState this.Input inputIndexes this.CaretIndex
            let input, caretIndex = 
                this.AutoCompletionItems
                |> Seq.item this.SelectedAutoCompletionIndex
                |> acceptCompletionValue this.Input inputState
            this.Input <- input
            this.CaretIndex <- caretIndex
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
    member this.FocusAutoCompletion() =
        this.IsAutoCompletionVisible <- true
        this.SetSelectedAutoCompletionIndex 0

    member val CommandHistoryItems = commandHistory with get,set
    member val SelectedCommandHistoryIndex = -1 with get,set
    member val IsCommandHistoryVisible = false with get,set
    member this.UpdateCommandHistory() =
        this.CommandHistoryItems <-
            commandHistory
            |> Seq.filter (fun command -> command |> Util.String.containsIgnoreCase this.Input)
    member this.AcceptCommandHistoryValue() = 
        this.Input <- this.CommandHistoryItems |> Seq.item this.SelectedCommandHistoryIndex
        this.CaretIndex <- this.Input.Length
    member this.CommandHistoryPreviousItem() =
        if (this.CommandHistoryItems |> Seq.length) <> -1 then
            this.SelectedCommandHistoryIndex <-
                if this.SelectedCommandHistoryIndex > 0 then this.SelectedCommandHistoryIndex - 1
                else (this.CommandHistoryItems |> Seq.length) - 1
    member this.CommandHistoryNextItem() =
        if this.SelectedCommandHistoryIndex < (this.CommandHistoryItems |> Seq.length) - 1 then
            this.SelectedCommandHistoryIndex <- this.SelectedCommandHistoryIndex + 1
        elif this.CommandHistoryItems |> Seq.length > 0 then
            this.SelectedCommandHistoryIndex <- 0
    member this.FocusCommandHistory() =
        this.IsCommandHistoryVisible <- true
        this.UpdateCommandHistory()
        this.SelectedCommandHistoryIndex <- (this.CommandHistoryItems |> Seq.length) - 1

    member val CommandExecuted = Event<string>() with get
    member this.Run() =
        if this.Input <> "" then
            addCommandHistory this.Input
            match tryFindCommandDefinition commandsGroups this.Input inputIndexes.GroupName inputIndexes.CommandName with
            | Some command -> 
                let run() =
                    let commandId = System.Guid.NewGuid()
                    let inputArguments = parseCommandArguments this.Input inputIndexes
                    let runOptions: RunOptions = { CommandArguments = inputArguments }
                    let input = this.Input
                    this.Input <- ""
                    try
                        Diagnostics.infoWithData "Started command" [ "Id", commandId; "Input", input ]
                        command.Run runOptions
                        Diagnostics.infoWithData "Finished command" [ "Id", commandId; "Input", input ]
                    with error -> 
                        Diagnostics.exceptWithData error ["Command", this.Input]
                if command.AsyncExecutionEnabled then async { run() } |> Async.Start
                else run()
            | None -> this.Input <- ""
        this.CommandExecuted.Trigger this.Input

