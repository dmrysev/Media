module Media.UI.Core.Command

open Media
open System

let initCommand<'a> (execute: 'a -> unit) =
    let event = Event<_,_>()
    { 
    new System.Windows.Input.ICommand with
        member this.CanExecute (param : obj) = true
        member this.Execute (param : obj) = 
            try execute (param :?> 'a)
            with ex -> API.Diagnostics.except ex

        [< CLIEvent >]
        member this.CanExecuteChanged = event.Publish 
    }

let initCommand2<'a> (execute: 'a -> unit) (canExecute: 'a  -> bool) =
    let event = Event<_,_>()
    { 
    new System.Windows.Input.ICommand with
        member this.CanExecute (param : obj) = canExecute (param :?> 'a)
        member this.Execute (param : obj) = 
            try execute (param :?> 'a)
            with ex -> API.Diagnostics.except ex

        [< CLIEvent >]
        member this.CanExecuteChanged = event.Publish 
    }
