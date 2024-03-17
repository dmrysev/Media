namespace Media.API

open Media
open Util.Path
open System

module LocalMedia =
    type LimitResultOptions = { StartIndex: int; MaxResultCount: int }

    type FormatVideoOptions = {
        InputFilePath: FilePath
        TimestampRanges: Util.Time.Range seq }

    type VideoSlideShow = {
        Id: System.Guid
        SeekRelative: TimeSpan -> unit
        SeekAbsolute: TimeSpan -> unit
        Start: unit -> unit
        Stop: unit -> unit }

    type ImportOptions = 
        | Video of ImportVideoOptions
        | ImageSets of ImportImageSetsOptions
    and ImportVideoOptions = {
        Source: DirectoryPath
        Destination: DirectoryPath }
    and ImportImageSetsOptions = {
        Source: DirectoryPath
        Destination: DirectoryPath
        RemoveFileMetaData: bool }

    type FindOptions = {
        Filter: {| 
            Tags: string seq option
            TagsAnyOf: string seq option
            ExcludeTags: string seq option
            Locations: DirectoryPath seq option
            TitleContains: string option
            Authors: string seq option
            Characters: string seq option
            Languages: string seq option
            PathType: PathType option |}
        Sort: SortOptions
        LimitResult: LimitResultOptions option }
        with static member Default = {
                Filter = {| 
                    Tags = None
                    TagsAnyOf = None
                    ExcludeTags = None
                    Locations = None
                    TitleContains = None
                    Authors = None
                    Characters = None
                    Languages = None
                    PathType = None |}
                Sort = DefaultSort
                LimitResult = None }
    and SortOptions = 
        | DefaultSort
        | DataAddTimeAscending
        | DataAddTimeDescending
        | MetaDataCreationTimeAscending
        | MetaDataCreationTimeDescending
        | DataModificationTimeAscending
        | DataModificationTimeDescending
        | PathAscending
        | PathDescending
        | SizeAscending
        | SizeDescending
        | TitleAscending
        | TitleDescending
    and PathType = DirectoryPathType | FilePathType

    type ReplaceTagOptions = { Old: string; New: string }

    type Data = {
        MediaLocation: DirectoryPath
        OpenOne: Media.Id -> unit
        OpenMany: Media.Id seq -> unit
        Delete: Media.Id -> unit
        DeleteFile: FilePath -> unit
        FormatVideo: FormatVideoOptions -> unit
        InitVideoSlideShow: FilePath -> VideoSlideShow
        Import: unit -> unit }

    type Export = {
        ExportToEbook: Path -> DirectoryPath -> unit
        ExportDirectoryToEbook: ExportDirectoryToEbookOptions -> unit
        ExportVideoToEbook: ExportVideoToEbookOptions -> unit
        ExportToMobileDevice: Path -> DirectoryPath -> unit
        ReadCommonOutputLocations: unit -> DirectoryPath seq }
    and ExportDirectoryToEbookOptions = {
        InputDirectoryPath: DirectoryPath
        OutputDirectoryPath: DirectoryPath
        OutputName: string
        Shuffle: bool
        LimitResult: LimitResultOptions option }
    and ExportVideoToEbookOptions = {
        InputFilePath: FilePath
        OutputDirectoryPath: DirectoryPath
        OutputName: string
        TimeInterval: TimeSpan
        TimeRange: Util.Time.Range option }

    type MetaData = {
        ReadAll: unit -> Media.MetaData seq
        HasSource: Url -> bool
        Write: Media.MetaData -> unit
        Delete: Media.Id -> unit
        HasId: Media.Id -> bool
        FindById: Media.Id ->Media.MetaData
        Find: FindOptions -> Media.MetaData seq
        FindIds: FindOptions -> Media.Id seq
        UpdateCommonTags: unit -> unit
        ReadCommonTags: unit -> string seq
        AddCommonLocation: DirectoryPath -> unit
        ReadCommonLocations: unit -> DirectoryPath seq
        CreateRevision: string -> unit
        ReplaceTag: ReplaceTagOptions -> unit
        AddFavoriteTag: string -> unit
        ReadFavoriteTags: unit -> string seq
        Events: {| Changed: IEvent<Media.Id> |} }

    type Preview = {
        GenerateThumbnail: Media.Id -> unit
        ReadThumbnailBytes: Media.Id -> byte array option
        HasScreenshots: Media.Id -> bool
        GenerateScreenshots: Media.Id -> unit
        GeneratePreviews: Media.Id -> unit
        Delete: Media.Id -> unit
        ReadScreenshotsBytes: Media.Id -> byte[] seq
        GetThumbnailLocation: Media.Id -> FilePath }

type LocalMedia = {
    Data: LocalMedia.Data
    Export: LocalMedia.Export
    MetaData: LocalMedia.MetaData
    Preview: LocalMedia.Preview }
