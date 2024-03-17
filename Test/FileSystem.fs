module Media.Test.FileSystem

open Media
open Media.API
open Media.TestUtil
open Util
open Util.Path
open NUnit.Framework
open FsUnit

let outputDirPath = generateTemporaryFolder()
let appDataDirPath = outputDirPath/DirectoryName "appData"
let pluginDirPath = appDataDirPath/DirectoryName "plugin"
let initFileSystemDataAccess() = IO.FileSystem.Default.init appDataDirPath pluginDirPath

[<SetUp>]
let setUp () = 
    Util.IO.Directory.delete outputDirPath

[<TearDown>]
let tearDown () = 
    Util.IO.Directory.delete outputDirPath

[<Test>]
let ``Move file to trash bin``() =
    // ARRANGE
    let fileSystem = initFileSystemDataAccess()
    let filePath = outputDirPath/FilePath "test/path/file.txt"
    Util.IO.File.writeText filePath "my text"

    // ACT
    fileSystem.File.MoveToTrashBin filePath

    // ASSERT
    filePath |> Util.IO.File.exists |> should be False
    Util.IO.Directory.listFilesRecursive appDataDirPath
    |> Seq.exists (fun filePath -> filePath.Value |> Util.String.contains "test/path/file.txt" )
    |> should be True

[<Test>]
let ``Move directory to trash bin``() =
    // ARRANGE
    let fileSystem = initFileSystemDataAccess()
    let dirPath = Util.IO.Directory.initialize (outputDirPath/DirectoryPath "test/dir/path")
    Util.IO.Directory.ensureExists dirPath

    // ACT
    fileSystem.Directory.MoveToTrashBin dirPath

    // ASSERT
    dirPath |> Util.IO.Directory.exists |> should be False
    Util.IO.Directory.listDirectoriesRecursive appDataDirPath
    |> Seq.exists (fun dirPath -> dirPath.Value |> Util.String.contains "test/dir/path" )
    |> should be True
