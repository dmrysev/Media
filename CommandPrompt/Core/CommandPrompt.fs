module Media.Core.CommandPrompt

open Media.API
open Media.API.CommandPrompt
open Util.Path
open System

type State =
    | EnteringCommand of CommandGroupRange * CommandRange
    | EnteringPositionalValue of CommandGroupRange * CommandRange * Position * ValueRange
    | EnteringArgumentName of CommandGroupRange * CommandRange * ArgumentNameRange
    | EnteringArgumentValue of CommandGroupRange * CommandRange * ArgumentNameRange * ValueRange
and CommandGroupRange = int * int
and CommandRange = int * int
and ValueRange = int * int
and ArgumentNameRange = int * int
and Position = int

type InputIndexes = {
    GroupName: CommandGroupRange
    CommandName: CommandRange
    PositionValues: seq<ValueRange>
    Arguments: seq<ArgumentIndexes> }
and ArgumentIndexes = {
    Name: ArgumentNameRange
    Values: seq<ValueRange> }

let parseInputIndexes (input: string) =
    let inputEndIndex = input.Length - 1
    let rec tryFindIndex i char =
        if i = -1 || i > inputEndIndex then -1
        elif input[i] = char then i
        else tryFindIndex (i + 1) char
    let rec tryFindIndexNotEqual i char =
        if i = -1 || i > inputEndIndex then -1
        elif input[i] <> char then i
        else tryFindIndexNotEqual (i + 1) char
    let rec findValues startIndex values =
        let startIndex = tryFindIndexNotEqual startIndex ' '
        if startIndex = -1 || input[startIndex] = '-' then values
        else 
            let startIndex, valueEdgeChar =
                if input[startIndex] = ''' then startIndex + 1, '''
                else startIndex, ' '
            let i = tryFindIndex (startIndex + 1) valueEdgeChar
            let endIndex = if i <> -1 then i - 1 else inputEndIndex
            let values = [(startIndex, endIndex)] |> Seq.append values
            let jump = if valueEdgeChar = ''' then 2 else 1
            findValues (endIndex + jump) values
    let rec findArguments startIndex values =
        let startIndex = tryFindIndex startIndex '-'
        if startIndex = -1 || startIndex = inputEndIndex then values
        else 
            let argumentNameStart = startIndex + 1
            let argumentNameEnd =
                let i = tryFindIndex argumentNameStart ' '
                if i <> -1 then i - 1 else inputEndIndex
            let valuesStartIndex = tryFindIndexNotEqual (argumentNameEnd + 1) ' '
            let argumentValues = findValues valuesStartIndex Seq.empty
            let value: ArgumentIndexes = { Name = (argumentNameStart, argumentNameEnd); Values = argumentValues }
            let values =  [value] |> Seq.append values
            let lastEndIndex =
                if argumentValues |> Seq.isEmpty |> not then (argumentValues |> Seq.last |> snd) + 1
                else argumentNameEnd + 1
            findArguments lastEndIndex values
    let groupNameStartIndex = if input.Length = 0 then -1 else 0
    let groupNameEndIndex =
        let i = tryFindIndex 0 '.'
        if i <> -1 then i - 1
        else inputEndIndex
    let commandStartIndex = 
        if groupNameEndIndex = -1 || groupNameEndIndex + 2 > inputEndIndex then -1
        else groupNameEndIndex + 2
    let commandEndIndex = 
        if commandStartIndex = -1 then -1
        else 
            let i = tryFindIndex commandStartIndex ' '
            if i <> -1 then i - 1 else inputEndIndex
    let positionValues = 
        if commandEndIndex <> - 1 then findValues (commandEndIndex + 1) Seq.empty
        else Seq.empty
    let argumentsStartIndex = 
        if positionValues |> Seq.isEmpty |> not then positionValues |> Seq.last |> snd
        else commandEndIndex
    let arguments =
        if commandEndIndex <> -1 then findArguments argumentsStartIndex Seq.empty
        else Seq.empty
    let indexes: InputIndexes = {
        GroupName = groupNameStartIndex, groupNameEndIndex
        CommandName = commandStartIndex, commandEndIndex
        PositionValues = positionValues |> Seq.cache
        Arguments = arguments |> Seq.cache }
    indexes

let parseInput (input: string) =
    let valueOrEmptyString (indexes: int * int) =
        if (fst indexes) = -1 || (snd indexes) = -1 then ""
        else input |> Util.String.slice (fst indexes) (snd indexes)
    let indexes = parseInputIndexes input
    let groupName = valueOrEmptyString indexes.GroupName
    let commandName = valueOrEmptyString indexes.CommandName
    let positionValues = indexes.PositionValues |> Seq.map valueOrEmptyString
    let arguments = 
        indexes.Arguments
        |> Seq.map (fun x -> 
            let name = x.Name |> valueOrEmptyString
            let values = x.Values |> Seq.map valueOrEmptyString
            name, values)
    groupName, commandName, positionValues, arguments

let determineInputState (input: string) (inputIndexes: InputIndexes) caretIndex =
    let inputEndIndex = input.Length - 1
    let caretIsWithin (startIndex, endIndex) = 
        startIndex <> - 1 && endIndex <> -1 &&
        startIndex <= caretIndex && caretIndex <= (endIndex + 1)
    let isEmptyRange (startIndex, endIndex) = startIndex = - 1 && endIndex = -1
    let positionValueIndexes =
        inputIndexes.PositionValues
        |> Util.Seq.tryFindItemBy caretIsWithin
    let argumentNameIndexes =
        inputIndexes.Arguments
        |> Util.Seq.tryFindItemBy (fun x -> caretIsWithin x.Name)
    let argumentValueIndexes =
        inputIndexes.Arguments
        |> Seq.collect (fun arg -> 
            arg.Values 
            |> Seq.map (fun v -> arg.Name, v))
        |> Util.Seq.tryFindItemBy (fun (argName, value) -> caretIsWithin value)
    let startingNewArgumentValue =
        inputIndexes.Arguments
        |> Seq.map (fun arg -> arg.Name)
        |> Util.Seq.tryFindItemBackBy (fun argName -> (snd argName) < caretIndex )
    let startingNewPositionValue = 
        caretIsWithin inputIndexes.GroupName |> not
        && caretIsWithin inputIndexes.CommandName |> not
    let startingNewArgumentName = caretIndex - 1 > (snd inputIndexes.CommandName) && input[caretIndex - 1] = '-'
    let getPositionalValueCount inputPositionValue =
        let currentValueStartIndex = fst inputPositionValue
        let currentValueEndIndex = snd inputPositionValue
        if inputIndexes.PositionValues |> Seq.isEmpty then 0
        elif currentValueEndIndex <> -1 then
            inputIndexes.PositionValues
            |> Seq.findIndex (fun valueRange -> 
                let startIndex = fst valueRange
                currentValueStartIndex = startIndex)
        else
            match
                inputIndexes.PositionValues
                |> Seq.tryFindIndex (fun valueRange -> 
                    let startIndex = fst valueRange
                    currentValueStartIndex < startIndex)
            with
            | Some index -> index
            | None -> inputIndexes.PositionValues |> Seq.length    
    if inputIndexes.GroupName |> isEmptyRange 
        || inputIndexes.CommandName |> isEmptyRange 
        || caretIsWithin inputIndexes.GroupName
        || caretIsWithin inputIndexes.CommandName
        then State.EnteringCommand (inputIndexes.GroupName, inputIndexes.CommandName)
    elif argumentValueIndexes.IsSome then
        State.EnteringArgumentValue (inputIndexes.GroupName, inputIndexes.CommandName, argumentValueIndexes.Value |> fst, argumentValueIndexes.Value |> snd)
    elif argumentNameIndexes.IsSome then
        State.EnteringArgumentName (inputIndexes.GroupName, inputIndexes.CommandName, argumentNameIndexes.Value.Name)
    elif positionValueIndexes.IsSome then
        let position = getPositionalValueCount positionValueIndexes.Value
        State.EnteringPositionalValue (inputIndexes.GroupName, inputIndexes.CommandName, position, positionValueIndexes.Value)
    elif startingNewArgumentName then
        State.EnteringArgumentName (inputIndexes.GroupName, inputIndexes.CommandName, (caretIndex, -1))
    elif startingNewArgumentValue.IsSome then
        State.EnteringArgumentValue (inputIndexes.GroupName, inputIndexes.CommandName, startingNewArgumentValue.Value, (caretIndex, -1))
    elif startingNewPositionValue then
        let positionValueIndexes = (caretIndex, -1)
        let position = getPositionalValueCount positionValueIndexes
        State.EnteringPositionalValue (inputIndexes.GroupName, inputIndexes.CommandName, position, positionValueIndexes)
    else State.EnteringCommand (inputIndexes.GroupName, inputIndexes.CommandName)

let inputSlice input (startIndex, endIndex) = input |> Util.String.slice startIndex endIndex

let inputSliceOrEmpty (input: string) (startIndex, endIndex) =
    let inputEndIndex = input |> Util.Seq.lastIndex
    if startIndex = -1 || endIndex = -1 || startIndex = inputEndIndex then ""
    else input |> Util.String.slice startIndex endIndex

let tryFindCommandDefinition (commandsGroups: CommandsGroup seq) (input: string) groupNameRange commandNameRange =
    let inputSliceOrEmpty = inputSliceOrEmpty input
    let groupName = inputSliceOrEmpty groupNameRange
    let commandName = inputSliceOrEmpty commandNameRange
    let commandsGroup = commandsGroups |> Util.Seq.tryFindItemBy (fun commandsGroup -> commandsGroup.Name = groupName)
    match commandsGroup with
    | Some commandsGroup -> commandsGroup.Commands |> Util.Seq.tryFindItemBy (fun c -> c.Name = commandName)
    | None -> None

let tryFindPositionalArgumentDefinition (commandsGroups: CommandsGroup seq) (input: string) groupNameRange commandNameRange position =
    match tryFindCommandDefinition commandsGroups input groupNameRange commandNameRange with
    | Some command -> 
        command.ArgumentDefinitions
        |> Seq.tryFind (fun argDef -> 
            argDef.Position.IsSome && argDef.Position.Value = position)
    | None -> None

let tryFindArgumentDefinition (commandsGroups: CommandsGroup seq) (input: string) groupNameRange commandNameRange argumentNameRange =
    let argumentName = inputSlice input argumentNameRange
    match tryFindCommandDefinition commandsGroups input groupNameRange commandNameRange with
    | Some command -> 
        command.ArgumentDefinitions
        |> Seq.tryFind (fun argDef -> argDef.Position.IsNone && argDef.Name = argumentName)
    | None -> None

let parseCommandArguments (commandsGroups: CommandsGroup seq) (input: string) (inputIndexes: InputIndexes) =
    let inputSlice = inputSlice input
    let tryFindInputArgument argDefName =
        inputIndexes.Arguments
        |> Seq.tryFind (fun inputArg -> 
            let inputArgName = inputSlice inputArg.Name
            inputArgName = argDefName)
    match tryFindCommandDefinition commandsGroups input inputIndexes.GroupName inputIndexes.CommandName with
    | Some command ->
        let positionValueArguments =
            command.ArgumentDefinitions
            |> Seq.filter (fun argDef -> argDef.Position.IsSome)
            |> Seq.sortBy (fun argDef -> argDef.Position.Value)
            |> Seq.zip inputIndexes.PositionValues
            |> Seq.map (fun (posValRange, argDef) ->
                let value = inputSlice posValRange
                { argDef with Values = [value] } )
        let valueArguments =
            command.ArgumentDefinitions
            |> Seq.filter (fun argDef -> argDef.Position.IsNone)
            |> Seq.map (fun argDef -> argDef, (tryFindInputArgument argDef.Name))
            |> Seq.filter (fun (argDef, inputArg) -> inputArg.IsSome)
            |> Seq.map (fun (argDef, inputArg) ->
                let values = inputArg.Value.Values |> Seq.map inputSlice
                { argDef with Values = values })
        Seq.concat [positionValueArguments; valueArguments]
    | None -> Seq.empty

let initAutoCompletionItems (commandsGroups: CommandsGroup seq) suggestionsArguments input (inputState: State) =
    let inputSliceOrEmpty = inputSliceOrEmpty input
    let tryFindCommandDefinition = tryFindCommandDefinition commandsGroups input
    let getSuggestions (argDef: CommandArgumentDefinition) valueRange =
        let inputValue = inputSliceOrEmpty valueRange
        if argDef.SuggestionsOptions.CustomFilterEnabled then
            argDef.Suggestions suggestionsArguments
        else
            argDef.Suggestions suggestionsArguments
            |> Seq.filter (fun value -> value.Contains(inputValue, StringComparison.InvariantCultureIgnoreCase) )
    match inputState with
    | State.EnteringCommand (groupNameRange, commandNameRange) ->
        let groupNameRange = inputSliceOrEmpty groupNameRange
        let commandNameRange = inputSliceOrEmpty commandNameRange
        commandsGroups 
        |> Seq.collect (fun commandsGroup -> 
            commandsGroup.Commands
            |> Seq.map (fun command -> commandsGroup.Name, command.Name) )
        |> Seq.filter (fun (groupName, command) ->
            groupName.Contains(groupNameRange, StringComparison.InvariantCultureIgnoreCase)
            && command.Contains(commandNameRange, StringComparison.InvariantCultureIgnoreCase) )
        |> Seq.map (fun (groupName, command) -> $"{groupName}.{command}")
    | State.EnteringPositionalValue (groupNameRange, commandNameRange, position, valueRange) ->
        match tryFindPositionalArgumentDefinition commandsGroups input groupNameRange commandNameRange position with
        | Some argDef -> getSuggestions argDef valueRange
        | None -> Seq.empty
    | State.EnteringArgumentName (groupNameRange, commandNameRange, argumentsNameRange) ->
        let inputArgumentName = inputSliceOrEmpty argumentsNameRange
        match tryFindCommandDefinition groupNameRange commandNameRange with
        | Some command -> 
            let alreadyEnteredArguments =
                suggestionsArguments.InputArguments
                |> Seq.filter (fun argDef -> argDef.Position.IsNone)
                |> Seq.map (fun argDef -> argDef.Name)
            command.ArgumentDefinitions
            |> Seq.filter (fun argDef -> argDef.Position.IsNone && argDef.IsEnabled())
            |> Seq.map (fun argDef -> argDef.Name)
            |> Seq.except alreadyEnteredArguments
            |> Seq.filter (fun value -> value.Contains(inputArgumentName, StringComparison.InvariantCultureIgnoreCase) )
        | None -> Seq.empty
    | State.EnteringArgumentValue (groupNameRange, commandNameRange, argumentsNameRange, valueRange) ->
        match tryFindArgumentDefinition commandsGroups input groupNameRange commandNameRange argumentsNameRange with
        | Some argDef -> getSuggestions argDef valueRange
        | None -> Seq.empty

let acceptCompletionValue (commandsGroups: CommandsGroup seq) (input: string) (inputState: State) completionValue =
    let completionValue = 
        if completionValue |> Seq.contains ' ' 
        then $"'{completionValue}'" 
        else completionValue
    let updateInput (startIndex, endIndex) appendValue =
        let value = $"{completionValue}{appendValue}"
        let startIndex, endIndex =
            if input[startIndex - 1] = ''' && input[endIndex + 1] = ''' 
            then startIndex - 1, endIndex + 1
            else startIndex, endIndex
        let newInput =
            if startIndex = -1 || endIndex = -1 then input
            else input.Remove(startIndex, endIndex - startIndex + 1)    
            |> Util.String.insert startIndex value
        let newCaretIndex = startIndex + value.Length
        newInput, newCaretIndex
    match inputState with
    | State.EnteringCommand _ -> 
        let completionValue = $"{completionValue} "
        completionValue, completionValue.Length
    | State.EnteringPositionalValue (groupNameRange, commandNameRange, position, valueRange) -> 
        match tryFindPositionalArgumentDefinition commandsGroups input groupNameRange commandNameRange position with
        | Some argDef -> updateInput valueRange argDef.SuggestionsOptions.AutoAppendValueOnAccept
        | None -> updateInput valueRange SuggestionsOptions.Default.AutoAppendValueOnAccept
    | State.EnteringArgumentName (_, _, argumentNameRange) -> updateInput argumentNameRange SuggestionsOptions.Default.AutoAppendValueOnAccept
    | State.EnteringArgumentValue (groupNameRange, commandNameRange, argumentNameRange, valueRange) -> 
        match tryFindArgumentDefinition commandsGroups input groupNameRange commandNameRange argumentNameRange with
        | Some argDef -> updateInput valueRange argDef.SuggestionsOptions.AutoAppendValueOnAccept
        | None -> updateInput valueRange SuggestionsOptions.Default.AutoAppendValueOnAccept

let executeInputCommand (commandsGroups: CommandsGroup seq) (input: string) =
    let inputIndexes = parseInputIndexes input
    match tryFindCommandDefinition commandsGroups input inputIndexes.GroupName inputIndexes.CommandName with
    | Some command -> 
        let inputArguments = parseCommandArguments commandsGroups input inputIndexes
        let runOptions: RunOptions = { CommandArguments = inputArguments }
        command.Run runOptions
    | None -> ()
