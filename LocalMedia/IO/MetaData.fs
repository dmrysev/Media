module Media.IO.LocalMedia.MetaData

open Media
open Media.API
open Util.Path
open System

module DataAccess =
    let init (appDataDirPath: DirectoryPath) =
        let metaDataDirPath = appDataDirPath/DirectoryName "metadata"
        let commonTagsFilePath = appDataDirPath/FileName "common_tags"
        let favoriteTagsFilePath = appDataDirPath/FileName "favorite_tags"
        let commonLocationsFilePath = appDataDirPath/FileName "common_locations"
        let knownSourcesFilePath = appDataDirPath/FileName "known_sources"
        Util.IO.Directory.ensureExists metaDataDirPath
        Util.IO.File.ensureExists commonTagsFilePath
        Util.IO.File.ensureExists favoriteTagsFilePath
        Util.IO.File.ensureExists commonLocationsFilePath
        Util.IO.File.ensureExists knownSourcesFilePath
        let changedEvent = Event<Media.Id>()
        let jsonFileDataAccess = Util.DataAccess.JsonFileDataAccess(metaDataDirPath)
        let readAll() =
            jsonFileDataAccess.ReadAll()
            |> Seq.map Util.Json.fromJson<MetaData>
        let write (metaData: MetaData) = 
            metaData
            |> Util.Json.toJson
            |> jsonFileDataAccess.Write (Id.String metaData.Id)
            changedEvent.Trigger metaData.Id
            if metaData.Source <> Url.None then Util.IO.File.appendLine knownSourcesFilePath metaData.Source.Value
        let readCommonLocation() = 
            Util.IO.File.readAllLines commonLocationsFilePath
            |> Seq.map Util.Path.DirectoryPath
        let updateCommonTags() =
            readAll()
            |> Seq.collect (fun metaData -> metaData.Tags)
            |> Seq.distinct
            |> Seq.sort
            |> Util.IO.File.writeLines commonTagsFilePath
        let createRevision message =
            Util.VersionControl.addAll appDataDirPath
            Util.VersionControl.commit appDataDirPath message
        let creationTime id =
            Id.String id
            |> jsonFileDataAccess.GetEntryFilePath
            |> Util.IO.File.creationTime
        let find (findOptions: LocalMedia.FindOptions) =
            let sort (metaDataEntries: MetaData seq) =
                match findOptions.Sort with
                | LocalMedia.SortOptions.DefaultSort -> metaDataEntries
                | LocalMedia.SortOptions.MetaDataCreationTimeAscending ->
                    metaDataEntries
                    |> Seq.sortBy (fun metaData -> creationTime metaData.Id )
                | LocalMedia.SortOptions.DataAddTimeAscending ->
                    metaDataEntries
                    |> Seq.sortBy (fun metaData -> metaData.AddedDateTimeUTC )
                | LocalMedia.SortOptions.DataAddTimeDescending ->
                    metaDataEntries
                    |> Seq.sortByDescending (fun metaData -> metaData.AddedDateTimeUTC )
                | LocalMedia.SortOptions.MetaDataCreationTimeDescending -> 
                    metaDataEntries
                    |> Seq.sortByDescending (fun metaData -> creationTime metaData.Id )
                | LocalMedia.SortOptions.DataModificationTimeAscending -> 
                    metaDataEntries
                    |> Seq.sortBy (fun metaData -> 
                        match metaData.Path with
                        | File path -> Util.IO.File.modificationTime path
                        | Directory path -> Util.IO.Directory.modificationTime path )
                | LocalMedia.SortOptions.DataModificationTimeDescending -> 
                    metaDataEntries
                    |> Seq.sortByDescending (fun metaData -> 
                        match metaData.Path with
                        | File path -> Util.IO.File.modificationTime path
                        | Directory path -> Util.IO.Directory.modificationTime path )
                | LocalMedia.SortOptions.PathAscending ->
                    metaDataEntries
                    |> Seq.sortBy (fun metaData -> metaData.Path |> Util.Path.value)
                | LocalMedia.SortOptions.PathDescending -> 
                    metaDataEntries
                    |> Seq.sortByDescending (fun metaData -> metaData.Path |> Util.Path.value)
                | LocalMedia.SortOptions.SizeAscending -> 
                    metaDataEntries
                    |> Seq.sortBy (fun metaData -> 
                        match metaData.Path with
                        | File path -> Util.IO.File.size path
                        | Directory path -> Util.IO.Directory.size path )
                | LocalMedia.SortOptions.SizeDescending -> 
                    metaDataEntries
                    |> Seq.sortByDescending (fun metaData -> 
                        match metaData.Path with
                        | File path -> Util.IO.File.size path
                        | Directory path -> Util.IO.Directory.size path )
                | LocalMedia.SortOptions.TitleAscending ->
                    metaDataEntries
                    |> Seq.sortBy (fun metaData -> metaData.Title )
                | LocalMedia.SortOptions.TitleDescending ->
                    metaDataEntries
                    |> Seq.sortByDescending (fun metaData -> metaData.Title )
            readAll()
            |> Seq.filter (fun metaData -> 
                let hasRequiredTags = 
                    match findOptions.Filter.Tags with
                    | None -> true
                    | Some tags -> tags |> Util.Seq.isSubset metaData.Tags
                let hasAnyOfSpecifiedTags = 
                    match findOptions.Filter.TagsAnyOf with
                    | None -> true
                    | Some tags -> tags |> Util.Seq.hasOverlap metaData.Tags
                let hasExcludingTags = 
                    match findOptions.Filter.ExcludeTags with 
                    | None -> false
                    | Some excludingTags -> excludingTags |> Util.Seq.hasOverlap metaData.Tags
                let hasRequiredPathType =
                    match findOptions.Filter.PathType with
                    | None -> true
                    | Some pathType ->
                        match pathType with
                        | LocalMedia.DirectoryPathType -> match metaData.Path with Directory _ -> true | _ -> false
                        | LocalMedia.FilePathType -> match metaData.Path with File _ -> true | _ -> false
                let hasRequiredLocation =
                    match findOptions.Filter.Locations with 
                    | None -> true
                    | Some locations -> locations |> Seq.exists (fun location -> metaData.Path |> Util.Path.isInside location )
                let hasRequiredTitle =
                    match findOptions.Filter.TitleContains with
                    | None -> true
                    | Some titleSubstring -> metaData.Title.Contains (titleSubstring, StringComparison.InvariantCultureIgnoreCase)
                let hasRequiredAuthors = 
                    match findOptions.Filter.Authors with
                    | None -> true
                    | Some authors -> authors |> Util.Seq.isSubset metaData.Authors
                let hasRequiredCharacters = 
                    match findOptions.Filter.Characters with
                    | None -> true
                    | Some character -> character |> Util.Seq.isSubset metaData.Characters
                let hasRequiredLanguages = 
                    match findOptions.Filter.Languages with
                    | None -> true
                    | Some languages -> languages |> Util.Seq.isSubset metaData.Languages
                hasRequiredTags && hasAnyOfSpecifiedTags && (not hasExcludingTags) && hasRequiredPathType && hasRequiredLocation && 
                hasRequiredTitle && hasRequiredAuthors && hasRequiredCharacters && hasRequiredLanguages)
            |> sort
            |> fun items -> 
                match findOptions.LimitResult with
                | Some limit -> Util.Seq.limitItems limit.StartIndex limit.MaxResultCount items
                | None -> items
        let dataAccess: LocalMedia.MetaData = {
            ReadAll = readAll
            HasSource = fun url -> 
                Util.IO.File.readAllLines knownSourcesFilePath
                |> Seq.contains url.Value
            Write = write
            Delete = fun id -> jsonFileDataAccess.Delete (Id.String id)
            HasId = fun id -> jsonFileDataAccess.HasId (Id.String id)
            FindById = fun id -> jsonFileDataAccess.FindById (Id.String id) |> Util.Json.fromJson<MetaData>
            Find = find 
            FindIds = fun findOptions ->
                find findOptions
                |> Seq.map (fun x -> x.Id)
            UpdateCommonTags = updateCommonTags
            ReadCommonTags = fun _ -> Util.IO.File.readAllLines commonTagsFilePath
            AddCommonLocation = fun path -> 
                readCommonLocation()
                |> Seq.append [path]
                |> Seq.distinct
                |> Seq.map DirectoryPath.value
                |> Util.IO.File.writeLines commonLocationsFilePath
            ReadCommonLocations = fun _ -> readCommonLocation()
            CreateRevision = createRevision
            ReplaceTag = fun options -> 
                readAll()
                |> Seq.filter (fun x -> x.Tags |> Seq.contains options.Old)
                |> Seq.map (fun x -> 
                    let newTags = x.Tags |> Util.Seq.replace options.Old options.New
                    { x with Tags = newTags })
                |> Seq.iter write
                updateCommonTags()
                createRevision $"Replaced tag from {options.Old} to {options.New}"
            AddFavoriteTag = fun tag -> Util.IO.File.appendLine favoriteTagsFilePath tag
            ReadFavoriteTags = fun _ -> 
                Util.IO.File.readAllLines favoriteTagsFilePath
                |> Seq.filter (fun line -> line <> "")
            Events = {| Changed = changedEvent.Publish |} }
        dataAccess
