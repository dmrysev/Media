namespace Media.Application.Avalonia

open Media
open Avalonia.Controls
open Avalonia.Controls.Templates
open System

type ViewLocator() =
    interface IDataTemplate with
        
        member this.Build(data) =
            let viewModelTypeName = data.GetType().FullName
            let viewTypeName = 
                if viewModelTypeName = "Media.UI.Core.ImageViewModel" then "Media.Application.Avalonia.View.ImageView"
                else
                    viewModelTypeName
                    |> Util.String.replace "Media.UI.Core" "Media.UI.View.Avalonia"
                    |> Util.String.replace "ViewModel" "View"
                    |> Util.String.replace "+" "."
            let typ = Type.GetType(viewTypeName)
            if isNull typ then
                upcast TextBlock(Text = sprintf "Not Found: %s" viewTypeName)
            else
                downcast Activator.CreateInstance(typ)

        member this.Match(data) = data :? UI.Core.ViewModelBase
