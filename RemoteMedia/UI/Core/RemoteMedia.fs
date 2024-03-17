module Media.UI.Core.RemoteMedia

open Media
open Media.API.CommandPrompt
open Util.Reactive
open Util.Path

let initCommandsGroup 
    (remoteMedia: API.RemoteMedia)
    (getSelectedMediaId: unit -> Media.Id)
    (getAllMetaData: unit -> MetaData seq) =
    let update = 
        { Command.Init "Update" (fun options -> 
            let args = options.CommandArguments
            let ids =
                if isFlagSet args "All" then
                    getAllMetaData()
                    |> Seq.filter (fun metaData -> metaData.IsComplete |> not )                    
                    |> Seq.map (fun x -> x.Id)
                else [ getSelectedMediaId() ]
            Async.Start(async { 
                try ids |> Seq.iter remoteMedia.Update
                with error -> API.Diagnostics.except error } ) )
            with ArgumentDefinitions = [ 
                CommandArgumentDefinition.Init "All" ] }
    let commandsGroup: CommandsGroup = {
        Name = "RemoteMedia"
        Commands = [ update ] }
    commandsGroup
