namespace Media.UI.View.Avalonia.LocalMedia.Browser

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type PreviewView () as this = 
    inherit UserControl ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
