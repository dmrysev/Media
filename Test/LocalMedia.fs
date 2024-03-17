module Media.Test.LocalMedia

open Media
open Media.API
open Media.TestUtil
open Util.Path
open NUnit.Framework
open FsUnit

let outputDirPath = generateTemporaryFolder()
let appDataDirPath = outputDirPath/DirectoryName "appdata"
let pluginDirPath = appDataDirPath/DirectoryName "plugin"
let initMetaDataAccess() = IO.LocalMedia.MetaData.DataAccess.init (appDataDirPath/DirectoryName "metadata")
let initTestMetaData() = 
    let guid = System.Guid.NewGuid()
    let path = FilePath "/test/path/{guid}.jpg" |> Path.File
    MetaData.New guid path

let addMedia 
    (localMedia: API.LocalMedia) 
    (fileSystem: API.FileSystem)
    (guidString: string) =
    let guid = System.Guid.Parse guidString
    let path = outputDirPath/FilePath $"data/{guidString}.jpg" 
    fileSystem.File.EnsureExists path
    let metaData = MetaData.New guid (path |> Path.File)
    localMedia.MetaData.Write metaData

[<SetUp>]
let setUp () = 
    Util.IO.Directory.delete outputDirPath

[<TearDown>]
let tearDown () = 
    Util.IO.Directory.delete outputDirPath

[<Test>]
let ``If no meta data was added, reading meta data, must return empty sequence``() =
    let localMediaMetaData = initMetaDataAccess()
    localMediaMetaData.ReadAll() |> should equal []

[<Test>]
let ``Finding existing meta data by id, must return that meta data``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = initTestMetaData()
    let metaData2 = initTestMetaData()
    localMediaMetaData.Write metaData1
    localMediaMetaData.Write metaData2

    // ACT
    let result = localMediaMetaData.FindById metaData1.Id

    // ASSERT
    result |> should equal metaData1

[<Test>]
let ``Finding meta data by a tag, must return all meta data that have that tag``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"] }
    let metaData2 = { initTestMetaData() with Tags = ["tag_2"; "tag_3"] }
    let metaData3 = { initTestMetaData() with Tags = ["tag_3"; "tag_4"] }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with Tags = Some ["tag_2"] |}
            LimitResult = None }
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 2
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData1.Id; metaData2.Id]

[<Test>]
let ``Finding meta data by multiple tags, must return all meta data that have all specified tags``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"] }
    let metaData2 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"; "tag_3"] }
    let metaData3 = { initTestMetaData() with Tags = ["tag_2"; "tag_3"; "tag_4"] }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with Tags = Some ["tag_1"; "tag_2"] |}
            LimitResult = None } 
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 2
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData1.Id; metaData2.Id ]


[<Test>]
let ``Finding meta data by filtering with any tags, must return meta data that has any of specified tags``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"; "tag_4"] }
    let metaData2 = { initTestMetaData() with Tags = ["tag_2"; "tag_3"; "tag_4"] }
    let metaData3 = { initTestMetaData() with Tags = ["tag_3"; "tag_4"; "tag_5"] }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with TagsAnyOf = Some ["tag_1"; "tag_2"] |}
            LimitResult = None } 
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 2
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData1.Id; metaData2.Id ]

[<Test>]
let ``Finding meta data by excluding a tag, must return all meta data that don't have that tag``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"] }
    let metaData2 = { initTestMetaData() with Tags = ["tag_2"; "tag_3"] }
    let metaData3 = { initTestMetaData() with Tags = ["tag_3"; "tag_4"] }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with ExcludeTags = Some ["tag_2"] |}
            LimitResult = None }
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 1
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData3.Id ]

[<Test>]
let ``Finding meta data by provding multiple excluding tags, must return all meta data that don't have any of those tags``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"] }
    let metaData2 = { initTestMetaData() with Tags = ["tag_3"; "tag_4"] }
    let metaData3 = { initTestMetaData() with Tags = ["tag_5"; "tag_6"] }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with ExcludeTags = Some ["tag_2"; "tag_3"] |}
            LimitResult = None }
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 1
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData3.Id ]

[<Test>]
let ``Finding meta data by provding both including and excluding tags, must return all meta data that has including tags but not excluding``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"] }
    let metaData2 = { initTestMetaData() with Tags = ["tag_2"; "tag_3"] }
    let metaData3 = { initTestMetaData() with Tags = ["tag_2"; "tag_4"] }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| 
                LocalMedia.FindOptions.Default.Filter with 
                    Tags = Some ["tag_2"]
                    ExcludeTags = Some ["tag_3"] |}
            LimitResult = None }
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 2
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData1.Id; metaData3.Id ]

[<Test>]
let ``Finding meta data by data location, must return all meta data that have data path starting with specified location``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Path = FilePath "/some/path_1/file_1" |> Path.File }
    let metaData2 = { initTestMetaData() with Path = FilePath "/some/path_1/file_2" |> Path.File }
    let metaData3 = { initTestMetaData() with Path = FilePath "/some/path_2/file_3" |> Path.File }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with Locations = Some [ DirectoryPath "/some/path_1"] |}
            LimitResult = None } 
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 2
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData1.Id; metaData2.Id ]

[<Test>]
let ``Finding meta data by multiple data locations, must return any meta data that have data path starting with one of specified location``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Path = FilePath "/some/path_1/file_1" |> Path.File }
    let metaData2 = { initTestMetaData() with Path = FilePath "/some/path_2/file_2" |> Path.File }
    let metaData3 = { initTestMetaData() with Path = FilePath "/some/path_3/file_3" |> Path.File }
    [ metaData1; metaData2; metaData3 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            Filter = {| LocalMedia.FindOptions.Default.Filter with Locations = Some [ DirectoryPath "/some/path_1"; DirectoryPath "/some/path_2"] |}
            LimitResult = None } 
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal 2
    result |> Seq.map (fun x -> x.Id) |> should equivalent [ metaData1.Id; metaData2.Id ]

[<TestCase([|0; 0; 0|])>]
[<TestCase([|0; 1; 1|])>]
[<TestCase([|0; 2; 2|])>]
[<TestCase([|0; 3; 3|])>]
[<TestCase([|0; 4; 3|])>]
[<TestCase([|1; 1; 1|])>]
[<TestCase([|1; 2; 2|])>]
[<TestCase([|1; 3; 2|])>]
[<TestCase([|5; 3; 0|])>]
let ``Finding meta data and specifying result limit``(limitOptions: int array) =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let writeMessage() = 
        initTestMetaData() |> localMediaMetaData.Write
        System.Threading.Thread.Sleep 15
    for i in {1..3} do writeMessage()

    // ACT
    let findOptions = { 
        LocalMedia.FindOptions.Default with
            LimitResult = Some { StartIndex = limitOptions[0]; MaxResultCount = limitOptions[1] } } 
    let result = localMediaMetaData.Find findOptions

    // ASSERT
    result |> Seq.length |> should equal limitOptions[2]

[<Test>]
let ``If meta data with source was added, checking if meta data access has source, must return true``() =
    let localMediaMetaData = initMetaDataAccess()
    localMediaMetaData.Write { initTestMetaData() with Source = Url "https://source.com/view/id_1" }
    localMediaMetaData.HasSource (Url "https://source.com/view/id_1") |> should be True

[<Test>]
let ``If no meta data was added, checking if meta data access has source, must return false``() =
    let localMediaMetaData = initMetaDataAccess()
    localMediaMetaData.HasSource (Url "https://source.com/view/id_1") |> should be False

[<Test>]
let ``If meta data was added, updating common tags and then getting them, must return list of unique tags in meta database``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    localMediaMetaData.Write { initTestMetaData() with Tags = ["tag 1"; "tag 2"] }
    localMediaMetaData.Write { initTestMetaData() with Tags = ["tag 2"; "tag 3"] }

    // ACT
    localMediaMetaData.UpdateCommonTags()
    let tags = localMediaMetaData.ReadCommonTags()

    // ASSERT
    tags |> should equivalent ["tag 1"; "tag 2"; "tag 3"]

[<Test>]
let ``If meta data was added, checking if meta data access has id, must return true``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData = initTestMetaData()
    localMediaMetaData.Write metaData

    // ACT and ASSERT
    localMediaMetaData.HasId metaData.Id |> should be True

[<Test>]
let ``Writing meta data and when reading all, must return exactly the same records``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    let metaData1 = { initTestMetaData() with Tags = ["tag_1"; "tag_2"]; Source = Url "https://source.com/view/1111" }
    let metaData2 = { initTestMetaData() with Tags = ["tag_3"; "tag_4"]; Source = Url "https://source.com/view/2222" }
    [ metaData1; metaData2 ] |> Seq.iter localMediaMetaData.Write

    // ACT
    let metaDatabase = localMediaMetaData.ReadAll()

    // ASSERT
    metaDatabase |> Seq.length |> should equal 2
    metaDatabase |> Seq.exists (fun x -> x.Id = metaData1.Id) |> should be True
    metaDatabase |> Seq.exists (fun x -> x.Id = metaData2.Id) |> should be True
    metaDatabase |> Seq.find (fun x -> x.Id = metaData1.Id) |> should equal metaData1
    metaDatabase |> Seq.find (fun x -> x.Id = metaData2.Id) |> should equal metaData2

[<Test>]
let ``Adding common location and then reading them back, must return all added common locations``() =
    // ARRANGE
    let localMediaMetaData = initMetaDataAccess()
    localMediaMetaData.AddCommonLocation (DirectoryPath "/some/path_1")
    localMediaMetaData.AddCommonLocation (DirectoryPath "/some/path_2")

    // ACT
    let commonLocations = localMediaMetaData.ReadCommonLocations()

    // ASSERT
    commonLocations |> Seq.map DirectoryPath.value |> should equivalent [ "/some/path_1"; "/some/path_2" ]

[<Test>]
let ``If an item is deleted from MainViewModel, selected index must remain the same``() =
    // ARRANGE
    let fileSystem = IO.FileSystem.Default.init outputDirPath pluginDirPath
    let localMedia = IO.LocalMedia.Default.init appDataDirPath fileSystem
    let addMedia = addMedia localMedia fileSystem
    addMedia "9a29cd1f-3b7f-48d6-bdb8-579c29b3f233"
    addMedia "7ae65c47-51a5-4487-bdc6-93bc3648df34"
    addMedia "d6bf7fab-ba1f-4813-b473-bdbd37431628"
    addMedia "04b05dee-3095-4691-b10f-79e2bac83082" 
    let resource = Media.TestUtil.initFakeResourceDataAccess()
    let mainViewModel = UI.Core.LocalMedia.Browser.MainViewModel (localMedia, fileSystem, resource)
    let findOptions = API.LocalMedia.FindOptions.Default    
    mainViewModel.Search findOptions 0
    mainViewModel.SetSelectedItemIndex 2

    // ACT
    mainViewModel.Delete()

    // ASSERT
    mainViewModel.Items |> Seq.length |> should equal 3
    mainViewModel.GetSelectedItemIndex() |> should equal 2
