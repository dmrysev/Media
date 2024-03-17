namespace Media.Application.Avalonia.View

open Media
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Interactivity
open System.Threading.Tasks


type ImageView () as this = 
    inherit UserControl ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

        this.DataContextChanged.Add (fun _ ->
            if this.DataContext <> null then 
                match this.DataContext with
                | :? UI.Core.ImageViewModel -> 
                    let imageViewModel = this.DataContext :?> UI.Core.ImageViewModel
                    this.DataContext <- Application.Avalonia.Core.ImageViewModel (imageViewModel)
                | _ -> () )

