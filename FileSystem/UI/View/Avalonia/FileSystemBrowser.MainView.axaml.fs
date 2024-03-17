namespace Media.UI.View.Avalonia.FileSystem.Browser

open Media
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Interactivity

type MainView () as this = 
    inherit UserControl ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.DataContextChanged.Add (fun _ ->
            let viewModel = this.DataContext :?> UI.Core.FileSystem.Browser.MainViewModel
            let fileSystemEntriesListBox = this.FindControl<ListBox>("FileSystemEntriesListBox")
            fileSystemEntriesListBox.SelectionChanged.Add (fun _ ->
                viewModel.SetSelectedIndexes (fileSystemEntriesListBox.Selection.SelectedIndexes) )        )
