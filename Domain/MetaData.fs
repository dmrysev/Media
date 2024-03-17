namespace Media

open Util.Path

type Id = MediaId of System.Guid
with static member String (id: Id) = 
        let (MediaId guid) = id
        guid.ToString()

type MetaData = {
    Id: Id
    Path: Path
    Tags: string seq
    Title: string
    Authors: string seq
    Actors: string seq
    Characters: string seq
    Groups: string seq
    Languages: string seq
    Categories: string seq
    IsComplete: bool
    Source: Url
    AddedDateTimeUTC: System.DateTime }
with static member New (guid: System.Guid) (path: Path) = {
        Id = guid |> MediaId
        Path = path
        Tags = []
        Title = ""
        Authors = []
        Actors = []
        Characters = []
        Groups = []
        Languages = []
        Categories = []
        IsComplete = true
        Source = Url.None
        AddedDateTimeUTC = System.DateTime.UtcNow }
        