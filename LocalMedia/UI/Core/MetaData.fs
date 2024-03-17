module Media.UI.Core.LocalMedia.MetaData

open Media
open Media.API.CommandPrompt
open Util.Path
open Util.Reactive
open System

let initCommandsGroup 
    (getMetaData: unit -> MetaData)
    (writeMetaData: MetaData -> unit)
    (addFavoriteTag: string -> unit)
    sendInfo
    (focusInfoOutput: unit -> unit)
    (commonTags: string seq)
    (fileSystem: API.FileSystem) =
    let mutable autoShowSubscription: IDisposable option = None
    let showMetaData() =
        getMetaData()
        |> Util.Json.toJsonIndented
        |> sendInfo
    let addTags tags =
        let metaData = getMetaData()
        writeMetaData { metaData with Tags = tags |> Seq.append metaData.Tags }
    let removeTags tags =
        let metaData = getMetaData()
        writeMetaData { metaData with Tags = metaData.Tags |> Seq.except tags }
    let update = { 
        Command.Init "Update" (fun options -> 
            let args = options.CommandArguments
            match tryFindValues args "AddTags" with
            | Some tags -> addTags tags
            | None -> ()
            match tryFindValues args "RemoveTags" with
            | Some tags -> removeTags tags
            | None -> ()
            if isFlagSet args "MarkWatched" then removeTags [ "unwatched" ]
            if isFlagSet args "MarkFavorite" then addTags [ "favorite" ] )
        with 
            ArgumentDefinitions = [
                { CommandArgumentDefinition.Init "AddTags" with 
                    Suggestions = fun _ -> commonTags }
                { CommandArgumentDefinition.Init "RemoveTags" with 
                    Suggestions = fun _ -> getMetaData().Tags }
                CommandArgumentDefinition.Init "MarkWatched"
                CommandArgumentDefinition.Init "MarkFavorite" ]
            AsyncExecutionEnabled = true     }
    let markWatched = Command.Init "MarkWatched" (fun _ -> removeTags [ "unwatched" ] )
    let markFavorite = Command.Init "MarkFavorite" (fun _ -> addTags [ "favorite" ] )
    let unmarkFavorite = Command.Init "UnmarkFavorite" (fun _ -> removeTags [ "favorite" ] )
    let show = Command.Init "Show" (fun _ -> 
        showMetaData()
        focusInfoOutput() )
    let addFavoriteTag = { 
        Command.Init "AddFavoriteTag" (fun options -> 
            let tag = findValue options.CommandArguments "Tag"
            addFavoriteTag tag )
        with ArgumentDefinitions = [
            { CommandArgumentDefinition.Init "Tag" with 
                Position = Some 0
                Suggestions = fun _ -> commonTags } ]}
    let copyPath = Command.Init "CopyPath" (fun _ -> 
        let metaData = getMetaData()
        metaData.Path
        |> Util.Path.value
        |> fileSystem.Clipboard.SetText )
    let copySource = Command.Init "CopySource" (fun _ -> 
        let metaData = getMetaData()
        metaData.Source
        |> Url.value
        |> fileSystem.Clipboard.SetText )            
    let commandsGroup: CommandsGroup = {
        Name = "MetaData"
        Commands = [ 
            update; markWatched; markFavorite; unmarkFavorite; addFavoriteTag; show;
            copyPath; copySource ] }
    commandsGroup