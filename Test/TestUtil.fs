module Media.TestUtil

open Util.Path
open NUnit.Framework

let generateTemporaryFolder () = Util.IO.Directory.generateTemporaryDirectory()

type IOTest() = inherit CategoryAttribute()
type LongRunningTest() = inherit CategoryAttribute()

let downloadBinaryFake2 (url: Url) (outputFilePath: FilePath) =
    Util.IO.File.create outputFilePath
    
let initFakeResourceDataAccess() =
    let dataAccess: API.Resource = {
        ReadImageBytes = fun _ -> [| byte(0x00); byte(0x21); byte(0x60) |]    }
    dataAccess
 