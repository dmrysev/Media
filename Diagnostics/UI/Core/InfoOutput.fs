module Media.UI.Core.Diagnostics.InfoOutput

open Media
open Media.API.CommandPrompt
open System

type MainViewModel (
    newInfoMessage: IObservable<string>,
    geometryChanged: IObservable<Util.Drawing.Size>) as this =
    inherit UI.Core.ViewModelBase()
    let subscription = newInfoMessage.Subscribe this.AddText
    do
        API.Diagnostics.Error.subscriber.Add (fun details -> details |> Core.Diagnostics.formatErrorDetailsLog |> this.AddText)
        API.Diagnostics.Info.subscriber.Add (fun details -> details |> Core.Diagnostics.formatInfoDetailsLog |> this.AddText)    
        geometryChanged.Add (fun size -> 
            this.Height <- size.Height )
    member val Height = 600.0 with get,set
    member val IsFocused = false with get,set
    member val Text = "" with get,set
    member this.AddText (text: string) = 
        this.Text <- $"{text}\n{this.Text}"
    member this.Clear() = this.Text <- ""
    interface System.IDisposable with
        member this.Dispose() = 
            subscription.Dispose()

let initCommandsGroup (viewModel: MainViewModel) =
    let clear = 
        Command.Init "Clear" (fun options -> 
            let args = options.CommandArguments
            viewModel.Clear() )
    let commandsGroup: CommandsGroup = {
        Name = "InfoOutput"
        Commands = [ clear ] }
    commandsGroup