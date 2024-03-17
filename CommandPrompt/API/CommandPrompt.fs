module Media.API.CommandPrompt

open Util.Path
open System

type CommandsGroup = {
    Name: string
    Commands: Command seq }
and Command = {
    Name: string
    Run: RunOptions -> unit
    ArgumentDefinitions: CommandArgumentDefinition seq
    AsyncExecutionEnabled: bool }
with static member Init name funct = {
        Name = name
        Run = funct
        ArgumentDefinitions = Seq.empty
        AsyncExecutionEnabled = false }
and RunOptions = {
    CommandArguments: CommandArgumentDefinition seq }
and CommandArgumentDefinition = {
    Name: string
    Values: string seq
    Position: int option
    IsEnabled: unit -> bool
    Suggestions: SuggestionsArguments -> string seq
    SuggestionsOptions: SuggestionsOptions }
with static member Init name = {
        Name = name
        Values = Seq.empty
        Position = None
        IsEnabled = fun _ -> true
        Suggestions = fun _ -> Seq.empty
        SuggestionsOptions = SuggestionsOptions.Default   }
and SuggestionsArguments = {
    InputArguments: CommandArgumentDefinition seq }
and SuggestionsOptions = {
    CustomFilterEnabled: bool
    AutoAppendValueOnAccept: string   }
with static member Default = {
        CustomFilterEnabled = false
        AutoAppendValueOnAccept = " " }

let tryFindValues (arguments: CommandArgumentDefinition seq) (name: string) =
    match arguments |> Seq.tryFind (fun inputArg -> inputArg.Name = name) with
    | Some inputArg -> Some inputArg.Values
    | None -> None

let tryFindValue (arguments: CommandArgumentDefinition seq) (name: string) =
    match tryFindValues arguments name with
    | Some values -> values |> Seq.head |> Some
    | None -> None

let tryFindValuesOrEmpty (arguments: CommandArgumentDefinition seq) (name: string) =
    match tryFindValues arguments name with
    | Some values -> values
    | None -> Seq.empty

let findValue (arguments: CommandArgumentDefinition seq) (name: string) =
    match tryFindValues arguments name with
    | Some values -> values |> Seq.head
    | None -> raise (ArgumentException())

let findValues (arguments: CommandArgumentDefinition seq) (name: string) =
    match arguments |> Seq.tryFind (fun inputArg -> inputArg.Name = name) with
    | Some inputArg -> inputArg.Values
    | None -> raise (ArgumentException())

let findUnionValue<'a>(arguments: CommandArgumentDefinition seq) (name: string) =
    findValue arguments name |> Util.Reflection.Union.fromString<'a>

let tryFindUnionValue<'a>(arguments: CommandArgumentDefinition seq) (name: string) =
    match tryFindValue arguments name with
    | Some value -> Util.Reflection.Union.fromString<'a> value |> Some
    | None -> None

let findValueOrDefault (arguments: CommandArgumentDefinition seq) name defaultValue =
    match tryFindValues arguments name with
    | Some values -> values |> Seq.head
    | None -> defaultValue

let findRecordValue<'a> (arguments: CommandArgumentDefinition seq) name =
    findValue arguments name |> Util.Json.fromJson<'a>

let tryFindRecordValue<'a> (arguments: CommandArgumentDefinition seq) name =
    match tryFindValue arguments name with
    | Some value -> Util.Json.fromJson<'a> value |> Some
    | None -> None

let isFlagSet (arguments: CommandArgumentDefinition seq) name =
    arguments |> Seq.exists (fun a -> a.Name = name)

let booleanSuggestion = fun _ -> 
    [ true; false ]
    |> Seq.map (fun x -> x.ToString() )
let tryFindBooleanValue (arguments: CommandArgumentDefinition seq) name =
    match tryFindValue arguments name with
    | Some value -> Boolean.Parse value |> Some
    | None -> None