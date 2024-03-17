module Media.IO.Resource

open Media
open Util.Path

let init () =
    let dataAccess: API.Resource = {
        ReadImageBytes = fun name ->
            Util.Environment.SpecialFolder.currentAssembly/FilePath $"Assets/{name}"
            |> Util.IO.File.readBytes   }
    dataAccess
