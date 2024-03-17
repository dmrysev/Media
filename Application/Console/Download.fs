module Media.UI.Console.Command.Download

open Media
open Util.Path
open CommandLine

[<Verb("download", HelpText = "Download media.")>]
type Options = {
    [<Option('q', "queue", Required = false, Default = false, 
     HelpText = "Put url to download queue instead of starting immediately. Daemon must be running.")>] 
    Queue: bool

    [<Option('c', "clipboard", Required = false, Default = false, 
     HelpText = "Get url from clipboard.")>] 
    Clipboard: bool

    [<Value(0, MetaName="url")>] 
    Url: string }

let run (opts: Options) (dep: API.Application.Dependency) =
    let url = 
        if opts.Clipboard then Util.IO.Clipboard.get() |> Url
        else Url opts.Url
    if opts.Queue then
        dep.RemoteMedia.QueueDownload url
    else 
        dep.RemoteMedia.Download url    

