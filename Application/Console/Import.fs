module Media.UI.Console.Command.Import

open Media
open Util.Path
open CommandLine

[<Verb("import", HelpText = "Import media")>]
type Options = { [<Option(Hidden = true)>] PlaceHolder: unit }

let run (opts: Options) (dep: API.Application.Dependency) =
    let localMedia = dep.LocalMedia
    localMedia.MetaData.CreateRevision "Before import"
    localMedia.Data.Import()
    localMedia.MetaData.UpdateCommonTags()
    localMedia.MetaData.CreateRevision "After import"
