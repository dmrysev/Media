module Media.API.Session

type Manager = {
    Load: string -> unit
    Save: string -> unit
    Delete: string -> unit
    Reset: unit -> unit }
