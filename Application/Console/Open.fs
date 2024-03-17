module Media.UI.Console.Command.Open

open Media
open Util
open Util.Path
open CommandLine

[<Verb("open", HelpText = "Open a media file.")>]
type Options = {
    [<Value(0, MetaName="path")>] 
    Path: string }

let run (opts: Options) (dep: API.Application.Dependency) =
    let path = opts.Path |> FilePath |> Util.IO.File.realPath
    let pathValue = path |> FilePath.fileName |> FileName.value
    if pathValue |> Util.StringMatch.isVideoFile then IO.FileSystem.Default.openVideoFile path
