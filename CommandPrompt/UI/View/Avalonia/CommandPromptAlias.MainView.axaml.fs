namespace Media.UI.View.Avalonia.CommandPromptAlias

open Media
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Markup.Xaml
open Avalonia.Interactivity

type MainView () as this = 
    inherit UserControl ()
    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

        this.DataContextChanged.Add (fun _ ->
            let viewModel = this.DataContext :?> UI.Core.CommandPromptAlias.MainViewModel
            viewModel.FocusInputEvent.Publish.Add this.FocusCommandPromptInput )

    member this.FocusCommandPromptInput() =
        let commandPromptInput = this.FindControl<TextBox>("CommandPromptInput")
        commandPromptInput.Focus() |> ignore

    // member this.FocusAutoCompletion() =
    //     let autoCompletion = this.FindControl<ListBox>("AutoCompletion")
    //     autoCompletion.Focus() |> ignore
    //     let viewModel = this.DataContext :?> MainViewModel
    //     viewModel.AutoCompletionNextItem()

    // member this.AcceptCompletionValue () =
    //     let commandPromptInput = this.FindControl<TextBox>("CommandPromptInput")
    //     let viewModel = this.DataContext :?> MainViewModel
    //     viewModel.AcceptCompletionValue()
