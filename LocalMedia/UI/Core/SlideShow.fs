module Media.UI.Core.LocalMedia.SlideShow

open Media
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive
open System

type SlideShow = {
    Start: unit -> unit
    Stop: unit -> unit
    NextSlide: unit -> unit }
with static member Default = {
        Start = fun _ -> ()
        Stop = fun _ -> ()
        NextSlide = fun _ -> ()  }

type PlayState = Started | Playing | Paused | Stopped

type SlideShowModel(slideShow: SlideShow) =
    inherit UI.Core.ViewModelBase()
    let stopSeekEvent = Event<_>()
    member val TimeInterval = TimeSpan.FromSeconds(1) with get,set
    member val Timeout = TimeSpan.FromSeconds(10) with get,set
    member val IsPlayPauseEnabled = false with get,set
    member val PlayState = Stopped with get,set
    member this.OnPlayStateChanged() =
        match this.PlayState with
        | Started -> 
            slideShow.Start()
            this.IsPlayPauseEnabled <- true
        | Playing -> 
            this.StartSeekTask()
        | Paused ->
            stopSeekEvent.Trigger()
        | Stopped ->
            slideShow.Stop()
            this.IsPlayPauseEnabled <- false
    member this.StartSeekTask() =
        let cancellationTokenSource = new System.Threading.CancellationTokenSource()
        stopSeekEvent.Publish.Add (fun _ -> cancellationTokenSource.Cancel())
        let mainThreadContext = System.Threading.SynchronizationContext.Current
        Async.Start(async { 
            while cancellationTokenSource.IsCancellationRequested |> not do 
                do! Util.Async.sleep this.Timeout
                do! Async.SwitchToContext mainThreadContext
                try slideShow.NextSlide()
                with e -> API.Diagnostics.except e
                do! Async.SwitchToThreadPool()  },
            cancellationTokenSource.Token)
    member this.ToggleStartStop() = 
        match this.PlayState with
        | Started | Paused ->
            this.PlayState <- Stopped
        | Playing -> 
            this.PlayState <- Paused
            this.PlayState <- Stopped
        | Stopped -> 
            this.PlayState <- Started
            this.PlayState <- Playing
    member this.Stop() =
        match this.PlayState with
        | Started | Paused ->
            this.PlayState <- Stopped
        | Playing -> 
            this.PlayState <- Paused
            this.PlayState <- Stopped
        | Stopped -> ()
    member this.TogglePlay() = 
        match this.PlayState with
        | Playing -> this.PlayState <- Paused
        | Paused -> this.PlayState <- Playing
        | _ -> ()
    interface System.IDisposable with
        member this.Dispose() = 
            this.PlayState <- Paused
            this.PlayState <- Stopped

let initCommandsGroup 
    (getSelectedPath: unit -> Path) 
    (initVideoSlideShow: FilePath -> API.LocalMedia.VideoSlideShow)
    nextImage =
    let mutable slideShowInstances = Map.empty<string, SlideShowModel>
    let start = 
        { Command.Init "Start" (fun options -> 
            let args = options.CommandArguments
            let interval = 
                match tryFindValue args "Interval" with
                | Some value -> Util.Json.fromJson<TimeSpan> value
                | None -> TimeSpan.FromSeconds(1)
            let timeout = 
                match tryFindValue args "Timeout" with
                | Some value -> Util.Json.fromJson<TimeSpan> value
                | None -> TimeSpan.FromSeconds(10)
            let selectedPath = getSelectedPath()
            let slideShow =
                match selectedPath with
                | File filePath -> 
                    if filePath |> FilePath.hasVideoExtension then
                        let videoSlideShow = initVideoSlideShow filePath
                        let slideShow: SlideShow = {
                            Start = videoSlideShow.Start
                            Stop = videoSlideShow.Stop
                            NextSlide = fun _ ->
                                try videoSlideShow.SeekRelative interval
                                with error -> API.Diagnostics.except error  }
                        slideShow
                    else raise (ArgumentException())
                | Directory dirPath -> { SlideShow.Default with NextSlide = nextImage }

            let slideShowModel = new SlideShowModel (slideShow)
            slideShowModel.Timeout <- timeout
            slideShowModel.ToggleStartStop()
            let selectedPathValue = selectedPath |> Util.Path.value
            slideShowInstances <- slideShowInstances |> Map.add selectedPathValue slideShowModel )
            with ArgumentDefinitions = [
                { CommandArgumentDefinition.Init "Interval" with 
                    IsEnabled = fun _ -> 
                        try 
                            match getSelectedPath() with
                            | File filePath -> filePath |> FilePath.hasVideoExtension
                            | _ -> false
                        with error -> false
                    Suggestions = fun _ -> [ 
                        TimeSpan.FromSeconds(1) |> Util.Json.toJson
                        TimeSpan.FromSeconds(2) |> Util.Json.toJson
                        TimeSpan.FromSeconds(5) |> Util.Json.toJson
                        TimeSpan.FromSeconds(10) |> Util.Json.toJson ]  }
                { CommandArgumentDefinition.Init "Timeout" with 
                    Suggestions = fun _ -> [ 
                        TimeSpan.FromSeconds(5) |> Util.Json.toJson
                        TimeSpan.FromSeconds(10) |> Util.Json.toJson
                        TimeSpan.FromSeconds(15) |> Util.Json.toJson ]  } ]}
    let stop = 
        Command.Init "Stop" (fun options -> 
            let args = options.CommandArguments
            let selectedPath = getSelectedPath() |> Util.Path.value
            let slideShowModel = slideShowInstances[selectedPath]
            slideShowModel.Stop() )
    let stopAll = 
        Command.Init "StopAll" (fun options -> 
            let args = options.CommandArguments
            slideShowInstances.Values
            |> Seq.iter (fun slideShowModel -> slideShowModel.Stop()) )
    let togglePlay = 
        Command.Init "TogglePlay" (fun options -> 
            let args = options.CommandArguments
            let selectedPath = getSelectedPath() |> Util.Path.value
            let slideShowModel = slideShowInstances[selectedPath]
            slideShowModel.TogglePlay() )
    let commandsGroup: CommandsGroup = {
        Name = "SlideShow"
        Commands = [ start; stop; stopAll; togglePlay ] }
    commandsGroup
