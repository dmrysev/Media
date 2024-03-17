module Media.Test.CommandPrompt

open Util.Path
open Media.API.CommandPrompt
open Media.Core.CommandPrompt
open NUnit.Framework
open FsUnit


[<Test>]
let ``Parse input``() =
    "GroupName" |> parseInput |> should equal ("GroupName", "", [], [])
    "GroupName.Command" |> parseInput |> should equal ("GroupName", "Command", [], [])

    "GroupName.Command PosValue1" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"], [])
    "GroupName.Command PosValue1 PosValue2" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "PosValue2"], [])
    "GroupName.Command 'Pos value 1'" |> parseInput |> should equal ("GroupName", "Command", ["Pos value 1"], [])
    "GroupName.Command 'Pos value 1' 'Pos value 2'" |> parseInput |> should equal ("GroupName", "Command", ["Pos value 1"; "Pos value 2"], [])
    "GroupName.Command PosValue1 'Pos value 2' PosValue3" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "Pos value 2"; "PosValue3"], [])

    "GroupName.Command -Arg1" |> parseInput |> should equal ("GroupName", "Command", [], ["Arg1", Seq.empty])
    "GroupName.Command -Arg1 -Arg2" |> parseInput |> should equal ("GroupName", "Command", [], ["Arg1", Seq.empty; "Arg2", Seq.empty])
    "GroupName.Command -Arg1 Value1" |> parseInput |> should equal ("GroupName", "Command", [], ["Arg1", ["Value1"]])
    "GroupName.Command -Arg1 Value1 Value2" |> parseInput |> should equal ("GroupName", "Command", [], ["Arg1", ["Value1"; "Value2"]])
    "GroupName.Command -Arg1 Value1 Value2 -Arg2 Value3 Value4" |> parseInput |> should equal ("GroupName", "Command", [], ["Arg1", ["Value1"; "Value2"]; "Arg2", ["Value3"; "Value4"]])
    "GroupName.Command -Arg1 Value1 'Some val 2' Value3" |> parseInput |> should equal ("GroupName", "Command", [], ["Arg1", ["Value1"; "Some val 2"; "Value3"]])

    "GroupName.Command PosValue1 PosValue2 -Arg1" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "PosValue2"], ["Arg1", Seq.empty])
    "GroupName.Command PosValue1 PosValue2 -Arg1 Value1" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "PosValue2"], ["Arg1", ["Value1"]])
    "GroupName.Command PosValue1 PosValue2 -Arg1 Value1 'Some val 2'" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "PosValue2"], ["Arg1", ["Value1"; "Some val 2"]])
    "GroupName.Command PosValue1 'Pos Value 2' -Arg1 Value1 'Some val 2'" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "Pos Value 2"], ["Arg1", ["Value1"; "Some val 2"]])

    // Edge cases
    "" |> parseInput |> should equal ("", "", [], [])
    "M" |> parseInput |> should equal ("M", "", [], [])
    "M.Command" |> parseInput |> should equal ("M", "Command", [], [])
    "GroupName.C" |> parseInput |> should equal ("GroupName", "C", [], [])
    "GroupName." |> parseInput |> should equal ("GroupName", "", [], [])
    "GroupName.Command " |> parseInput |> should equal ("GroupName", "Command", [], [])
    "GroupName.Command  " |> parseInput |> should equal ("GroupName", "Command", [], [])
    "GroupName.Command  PosValue1" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"], [])
    "GroupName.Command    PosValue1" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"], [])
    "GroupName.Command   PosValue1   PosValue2" |> parseInput |> should equal ("GroupName", "Command", ["PosValue1"; "PosValue2"], [])
    "GroupName.Command   'Pos value 1'    'Pos value 2'" |> parseInput |> should equal ("GroupName", "Command", ["Pos value 1"; "Pos value 2"], [])
    // "GroupName.Command 'Pos val" |> parseInput |> should equal ("GroupName", "Command", [], [])
    "GroupName.Command -" |> parseInput |> should equal ("GroupName", "Command", [], [])

[<Test>]
let ``Determine input state``() =
    let determineInputState input caretIndex =
        let inputIndexes = parseInputIndexes input
        determineInputState input inputIndexes caretIndex
    determineInputState "" 0 |> should equal (State.EnteringCommand ((-1, -1), (-1, -1)))
    determineInputState "Grp" 0 |> should equal (State.EnteringCommand ((0, 2), (-1, -1)))
    determineInputState "Grp" 1 |> should equal (State.EnteringCommand ((0, 2), (-1, -1)))
    determineInputState "Grp" 3 |> should equal (State.EnteringCommand ((0, 2), (-1, -1)))
    determineInputState "Grp." 4 |> should equal (State.EnteringCommand ((0, 2), (-1, -1)))
    determineInputState "Grp.C" 0 |> should equal (State.EnteringCommand ((0, 2), (4, 4)))
    determineInputState "Grp.C" 1 |> should equal (State.EnteringCommand ((0, 2), (4, 4)))
    determineInputState "Grp.C" 2 |> should equal (State.EnteringCommand ((0, 2), (4, 4)))
    determineInputState "Grp.C" 3 |> should equal (State.EnteringCommand ((0, 2), (4, 4)))
    determineInputState "Grp.C" 4 |> should equal (State.EnteringCommand ((0, 2), (4, 4)))
    determineInputState "Grp.C" 5 |> should equal (State.EnteringCommand ((0, 2), (4, 4)))
    determineInputState "Grp.Cmd" 4 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd" 5 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd" 6 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd" 7 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd " 7 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd V" 7 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd Val1" 7 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))

    determineInputState "Grp.Cmd " 8 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, -1)))
    determineInputState "Grp.Cmd V" 8 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 8)))
    determineInputState "Grp.Cmd Val1" 8 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))
    determineInputState "Grp.Cmd Val1" 9 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))
    determineInputState "Grp.Cmd Val1" 11 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))
    determineInputState "Grp.Cmd Val1" 12 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))
    determineInputState "Grp.Cmd Val1 " 13 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, -1)))
    determineInputState "Grp.Cmd Val1 V" 12 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))
    determineInputState "Grp.Cmd Val1 V" 13 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, 13)))
    determineInputState "Grp.Cmd Val1 Val2" 13 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, 16)))
    determineInputState "Grp.Cmd Val1 Val2" 15 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, 16)))
    determineInputState "Grp.Cmd Val1 Val2" 16 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, 16)))
    determineInputState "Grp.Cmd Val1 Val2" 17 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, 16)))
    determineInputState "Grp.Cmd Val1 -Arg1" 12 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))

    determineInputState "Grp.Cmd 'Val'" 12 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (9, 11)))
    determineInputState "Grp.Cmd 'Val '" 13 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (9, 12)))
    determineInputState "Grp.Cmd 'Val a'" 14 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (9, 13)))

    determineInputState "Grp.Cmd -" 9 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, -1)))
    determineInputState "Grp.Cmd -A" 9 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 9)))
    determineInputState "Grp.Cmd -A" 10 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 9)))
    determineInputState "Grp.Cmd -Arg1" 9 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 12)))
    determineInputState "Grp.Cmd -Arg1" 10 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 12)))
    determineInputState "Grp.Cmd -Arg1" 12 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 12)))
    determineInputState "Grp.Cmd -Arg1" 13 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 12)))
    determineInputState "Grp.Cmd Val1 -Arg1" 14 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (14, 17)))
    determineInputState "Grp.Cmd Val1 -" 14 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (14, -1)))
    determineInputState "Grp.Cmd -Arg1 Val1" 13 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (9, 12)))
    determineInputState "Grp.Cmd -Arg1 -" 15 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (15, -1)))
    determineInputState "Grp.Cmd -Arg1 -Arg2" 15 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (15, 18)))
    determineInputState "Grp.Cmd -Arg1 -Arg2" 17 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (15, 18)))
    determineInputState "Grp.Cmd -Arg1 -Arg2" 19 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (15, 18)))
    determineInputState "Grp.Cmd -Arg1 Val1 -" 20 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (20, -1)))
    determineInputState "Grp.Cmd -Arg1 Val1 -Arg2" 20 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (20, 23)))
    determineInputState "Grp.Cmd -Arg1 Val1 -Arg2" 22 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (20, 23)))
    determineInputState "Grp.Cmd -Arg1 Val1 -Arg2" 24 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (20, 23)))

    determineInputState "Grp.Cmd -Arg1 " 14 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, -1)))
    determineInputState "Grp.Cmd -Arg1 Val1" 14 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 17)))
    determineInputState "Grp.Cmd -Arg1 Val1" 15 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 17)))
    determineInputState "Grp.Cmd -Arg1 Val1" 15 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 17)))
    determineInputState "Grp.Cmd -Arg1 Val1" 17 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 17)))
    determineInputState "Grp.Cmd -Arg1 Val1 " 17 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 17)))
    determineInputState "Grp.Cmd -Arg1 Val1 " 18 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 17)))
    determineInputState "Grp.Cmd -Arg1 Val1 " 19 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (19, -1)))
    determineInputState "Grp.Cmd -Arg1 Val1 Val2" 19 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (19, 22)))
    determineInputState "Grp.Cmd -Arg1 Val1 -Arg2 " 25 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (20, 23), (25, -1)))
    determineInputState "Grp.Cmd -Arg1 Val1 -Arg2 Val2" 25 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (20, 23), (25, 28)))
    determineInputState "Grp.Cmd Val1 -Arg1 " 19 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (14, 17), (19, -1)))
    determineInputState "Grp.Cmd Val1 -Arg1 Val2" 19 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (14, 17), (19, 22)))

    determineInputState "Grp.Cmd -Arg1 'Val1'" 19 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (15, 18)))
    determineInputState "Grp.Cmd -Arg1 'Val1 '" 20 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (15, 19)))
    determineInputState "Grp.Cmd -Arg1 'Val1 a'" 21 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (15, 20)))

    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 0 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 7 |> should equal (State.EnteringCommand ((0, 2), (4, 6)))
    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 8 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 11)))
    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 14 |> should equal (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (14, 18)))
    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 22 |> should equal (State.EnteringArgumentName ((0, 2), (4, 6), (22, 25)))
    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 34 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (22, 25), (33 , 37)))
    determineInputState "Grp.Cmd Val1 'Val 2' -Arg1 Val3 'Val 4' -Arg2 Val5" 46 |> should equal (State.EnteringArgumentValue ((0, 2), (4, 6), (41, 44), (46 , 49)))

[<Test>]
let ``Initialize auto completion items``() =
    // ARRANGE
    let fakeCommand _ = ()
    let commandsGroups: CommandsGroup list = [
        { Name = "GroupA1"; Commands = [
            { Command.Init "Command1" fakeCommand
                with ArgumentDefinitions = [
                    { CommandArgumentDefinition.Init "PosArg1" with 
                        Position = Some 0
                        Suggestions = fun _ -> [ "PosArg1_Value1"; "PosArg1_Value2" ] }
                    { CommandArgumentDefinition.Init "PosArg2" with 
                        Position = Some 1
                        Suggestions = fun _ -> [ "PosArg2_Value1"; "PosArg2_Value2" ] }
                    { CommandArgumentDefinition.Init "PosArg3" with 
                        Position = Some 2
                        Suggestions = fun _ -> [ "PosArg3_Value1"; "PosArg3_Value2" ] } ]}
            Command.Init "Command2" fakeCommand ]}
        { Name = "GroupA2"; Commands = [
            { Command.Init "Command1" fakeCommand
                with ArgumentDefinitions = [
                    CommandArgumentDefinition.Init "Arg1"
                    CommandArgumentDefinition.Init "Arg2"
                    CommandArgumentDefinition.Init "Arg3" ] }]}
        { Name = "GroupB1"; Commands = [
            { Command.Init "Command1" fakeCommand
                with ArgumentDefinitions = [
                    { CommandArgumentDefinition.Init "Arg1" with 
                        Suggestions = fun _ -> [ "Value1"; "Value2" ] } ]} ]}
        { Name = "GroupC1"; Commands = [
            { Command.Init "Command1" fakeCommand
                with ArgumentDefinitions = [
                    { CommandArgumentDefinition.Init "PosArg1" with 
                        Position = Some 0
                        Suggestions = fun _ -> [ "PosArg1_Value1"; "PosArg1_Value2" ] } ]} ]} ]
    let init input = 
        let inputIndexes = parseInputIndexes input
        let caretIndex = input |> Seq.length
        let inputState = determineInputState input inputIndexes caretIndex
        let inputArguments = parseCommandArguments commandsGroups input inputIndexes
        let suggestionsArguments: SuggestionsArguments = { InputArguments = inputArguments }
        initAutoCompletionItems commandsGroups suggestionsArguments input inputState

    let initCaretPos input caretPos = 
        let inputIndexes = parseInputIndexes input
        let inputState = determineInputState input inputIndexes caretPos
        let inputArguments = parseCommandArguments commandsGroups input inputIndexes
        let suggestionsArguments: SuggestionsArguments = { InputArguments = inputArguments }
        initAutoCompletionItems commandsGroups suggestionsArguments input inputState

    // ACT & ASSERT
    init "GroupA" |> should equal [ "GroupA1.Command1"; "GroupA1.Command2"; "GroupA2.Command1" ]
    init "GroupA1" |> should equal [ "GroupA1.Command1"; "GroupA1.Command2" ]
    init "GroupB1" |> should equal [ "GroupB1.Command1" ]

    init "GroupA1." |> should equal [ "GroupA1.Command1"; "GroupA1.Command2" ]
    init "GroupA2." |> should equal [ "GroupA2.Command1" ]

    init "GroupA1.Command1 " |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    init "GroupA1.Command1 P" |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    init "GroupA1.Command1 PosArg1_Value1" |> should equal [ "PosArg1_Value1" ]
    init "GroupA1.Command1 PosArg1_Value1 " |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]
    init "GroupA1.Command1 PosArg1_Value1 P" |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]
    init "GroupA1.Command1 PosArg1_Value1 PosArg2_Value1" |> should equal [ "PosArg2_Value1" ]
    init "GroupA1.Command1 PosArg1_Value1 PosArg2_Value1 " |> should equal [ "PosArg3_Value1"; "PosArg3_Value2" ]

    init "GroupA2.Command1 -" |> should equal [ "Arg1"; "Arg2"; "Arg3" ]
    init "GroupA2.Command1 -Arg1 -" |> should equal [ "Arg2"; "Arg3" ]

    init "GroupB1.Command1 -Arg1 " |> should equal [ "Value1"; "Value2" ]
    init "GroupB1.Command1 -Arg1 V" |> should equal [ "Value1"; "Value2" ]

    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_" 17 |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_" 20 |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_" 25 |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_" 26 |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_" 30 |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_" 34 |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]

    initCaretPos "GroupA1.Command1  PosArg2_ PosArg3_" 17 |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_ PosArg3_" 25 |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]

    initCaretPos "GroupA1.Command1 PosArg1_  PosArg3_" 26 |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]
    initCaretPos "GroupA1.Command1 PosArg1_ PosArg2_ PosArg3_" 34 |> should equal [ "PosArg2_Value1"; "PosArg2_Value2" ]

    init "GroupC1.Command1 PosArg1_" |> should equal [ "PosArg1_Value1"; "PosArg1_Value2" ]
    init "GroupC1.Command1 PosArg1_Value1 " |> should equal [ ]

[<Test>]
let ``Accept auto completion value``() =
    // ARRANGE
    let acceptCompletionValue = acceptCompletionValue Seq.empty<CommandsGroup>

    // ACT & ASSERT
    acceptCompletionValue "Gr" (State.EnteringCommand ((0, 2), (-1, -1))) "GrpA.Cmd1" |> should equal ("GrpA.Cmd1 ", 10)
    acceptCompletionValue "Gr " (State.EnteringCommand ((0, 2), (-1, -1))) "GroupB.Cmd" |> should equal ("GroupB.Cmd ", 11)
    acceptCompletionValue "Gr.Cm" (State.EnteringCommand ((0, 2), (3, 4))) "GrpA.Cmd1" |> should equal ("GrpA.Cmd1 ", 10)
    acceptCompletionValue "Gr.Cm " (State.EnteringCommand ((0, 2), (3, 4))) "GroupB.Cmd" |> should equal ("GroupB.Cmd ", 11)

    acceptCompletionValue "Grp.Cmd Val" (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 10))) "Val1" |> should equal ("Grp.Cmd Val1 ", 13)
    acceptCompletionValue "Grp.Cmd  Val" (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (9, 11))) "Val 3" |> should equal ("Grp.Cmd  'Val 3' ", 17)
    acceptCompletionValue "Grp.Cmd 'Val '" (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (9, 12))) "Val with spaces" |> should equal ("Grp.Cmd 'Val with spaces' ", 26)
    acceptCompletionValue "Grp.Cmd 'Val w'" (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (9, 13))) "Val with spaces" |> should equal ("Grp.Cmd 'Val with spaces' ", 26)

    acceptCompletionValue "Grp.Cmd -Arg" (State.EnteringArgumentName ((0, 2), (4, 6), (9, 11))) "Arg1" |> should equal ("Grp.Cmd -Arg1 ", 14)
    acceptCompletionValue "Grp.Cmd  -Arg" (State.EnteringArgumentName ((0, 2), (4, 6), (10, 12))) "Argument3" |> should equal ("Grp.Cmd  -Argument3 ", 20)

    acceptCompletionValue "Grp.Cmd -Arg Val" (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 11), (13, 15))) "Val1" |> should equal ("Grp.Cmd -Arg Val1 ", 18)
    acceptCompletionValue "Grp.Cmd -Arg  Val" (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 11), (14, 16))) "Val1" |> should equal ("Grp.Cmd -Arg  Val1 ", 19)
    acceptCompletionValue "Grp.Cmd -Arg 'Val '" (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 11), (14, 17))) "Val with spaces" |> should equal ("Grp.Cmd -Arg 'Val with spaces' ", 31)

[<Test>]
let ``Accept auto completion value with custom append value``() =
    // ARRANGE
    let fakeCommand _ = ()
    let posArg1 = {
        CommandArgumentDefinition.Init "PosArg1" with
            Position = Some 0
            SuggestionsOptions = { 
                SuggestionsOptions.Default with
                    AutoAppendValueOnAccept = "" }   }
    let posArg2 = {
        CommandArgumentDefinition.Init "PosArg2" with
            Position = Some 1
            SuggestionsOptions = { 
                SuggestionsOptions.Default with
                    AutoAppendValueOnAccept = "/" }   }
    let arg1 = {
        CommandArgumentDefinition.Init "Arg1" with
            SuggestionsOptions = { 
                SuggestionsOptions.Default with
                    AutoAppendValueOnAccept = "" }   }
    let arg2 = {
        CommandArgumentDefinition.Init "Arg2" with
            SuggestionsOptions = { 
                SuggestionsOptions.Default with
                    AutoAppendValueOnAccept = "/" }   }
    let commandsGoup = { 
        Name = "Grp"
        Commands = [ 
            { Command.Init "Cmd" fakeCommand with ArgumentDefinitions = [ posArg1; posArg2; arg1; arg2 ] } ]      }
    let acceptCompletionValue = acceptCompletionValue [ commandsGoup ]

    // ACT & ASSERT
    acceptCompletionValue "Grp.Cmd Val" (State.EnteringPositionalValue ((0, 2), (4, 6), 0, (8, 10))) "Val1" |> should equal ("Grp.Cmd Val1", 12)
    acceptCompletionValue "Grp.Cmd Val1 Val" (State.EnteringPositionalValue ((0, 2), (4, 6), 1, (13, 15))) "Val2" |> should equal ("Grp.Cmd Val1 Val2/", 18)
    acceptCompletionValue "Grp.Cmd -Arg1 Val" (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 16))) "Val1" |> should equal ("Grp.Cmd -Arg1 Val1", 18)
    acceptCompletionValue "Grp.Cmd -Arg2 Val" (State.EnteringArgumentValue ((0, 2), (4, 6), (9, 12), (14, 16))) "Val1" |> should equal ("Grp.Cmd -Arg2 Val1/", 19)
