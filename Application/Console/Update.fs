module Media.UI.Console.Command.Update

open Media
open Util.Path
open CommandLine

[<Verb("update", HelpText = "Update known media.")>]
type Options = {
    [<Option('u', "urls", Required = false, 
     HelpText = "Update list of urls.")>] 
    Urls: string seq

    [<Option('a', "all", Required = false, Default = false, 
     HelpText = "Update all.")>] 
    IsUpdateAllSet: bool  }

let run (opts: Options) (dep: API.Application.Dependency) =
    let mediaMetaDataAccess = dep.LocalMedia.MetaData
    let metaDataEntries =
        if opts.IsUpdateAllSet then
            mediaMetaDataAccess.ReadAll()
            |> Seq.filter (fun metaData -> metaData.IsComplete |> not )
        else Seq.empty
    metaDataEntries
    |> Seq.iter (fun metaData -> dep.RemoteMedia.Update metaData.Id )
