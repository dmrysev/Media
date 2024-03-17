namespace Media.UI.Core

open Media
open System.Threading.Tasks

type ImageViewModel (getBytes: unit -> byte array, size: int) =
    inherit UI.Core.ViewModelBase()
    member val GetBytes = getBytes with get
    member val Size = size with get
    member val Interpolation = Some InterpolationQuality.Low with get,set
and InterpolationQuality = Low | Medium | High