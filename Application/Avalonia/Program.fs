namespace Media.Application.Avalonia

open Media
open Util.Path
open System
open Avalonia

module Program =
    [<CompiledName "BuildAvaloniaApp">] 
    let buildAvaloniaApp () = 
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace(areas = Array.empty)

    [<EntryPoint; STAThread>]
    let main argv =
        let appGuid = "1e9a73f1-bd41-49bf-803c-215964052796"
        let appDataDirPath = Util.Environment.SpecialFolder.applicationData/DirectoryName "media"
        let dependency, resources = IO.Application.init appDataDirPath appGuid argv
        App.Dependency <- Some dependency
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv) |> ignore
        App.Finalize.Trigger 0
        resources |> Seq.iter (fun d -> d.Dispose())
        0
