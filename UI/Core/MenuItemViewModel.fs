namespace Media.UI.Core

open Media.UI.Core.Command

type MenuItemViewModel = { 
    Header: string
    Items: MenuItemViewModel list
    Command: System.Windows.Input.ICommand
    CommandParameter: obj }
with static member Default = {
        Header = ""
        Items = []
        Command = initCommand (fun _ -> ())
        CommandParameter = null }