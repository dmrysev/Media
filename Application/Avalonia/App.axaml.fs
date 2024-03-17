namespace Media.Application.Avalonia

open Media
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml

type App() =
    inherit Application()

    static member val Dependency: API.Application.Dependency option = None with get,set
    static member val Finalize = Event<int>()

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let mainWindow = Application.Avalonia.View.MainWindow()
            let getMainViewSize() =
                let geometry: Util.Drawing.Size = {
                    Width = mainWindow.Width
                    Height = mainWindow.Height   }
                geometry
            let mainViewModel = Application.UI.Core.MainWindow.MainViewModel (App.Dependency.Value, getMainViewSize, App.Finalize.Publish)
            mainViewModel.CurrentMainContent <- Application.UI.Core.MainWindow.MainContent.MediaBrowser
            mainWindow.DataContext <- mainViewModel
            desktop.MainWindow <- mainWindow
        | :? ISingleViewApplicationLifetime as singleViewLifetime ->
            singleViewLifetime.MainView <- Application.Avalonia.View.MainView()
        | _ -> ()
        base.OnFrameworkInitializationCompleted()
