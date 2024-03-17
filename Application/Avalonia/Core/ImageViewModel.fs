namespace Media.Application.Avalonia.Core

open Media
open Media.UI.Core
open System.Threading.Tasks

type ImageViewModel (imageViewModel: UI.Core.ImageViewModel) =
    inherit UI.Core.ViewModelBase()
    let loadBitmap (bytes: byte array) size = 
        use stream = new System.IO.MemoryStream (bytes)
        match imageViewModel.Interpolation with
        | Some quality ->
            let bitmapQuality = 
                match quality with
                | InterpolationQuality.Low -> Avalonia.Media.Imaging.BitmapInterpolationMode.LowQuality
                | InterpolationQuality.Medium -> Avalonia.Media.Imaging.BitmapInterpolationMode.MediumQuality
                | InterpolationQuality.High -> Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality
            Avalonia.Media.Imaging.Bitmap.DecodeToHeight(stream, size, bitmapQuality)
        | None -> new Avalonia.Media.Imaging.Bitmap (stream)
        
    member val ImageBitmap = Task.Run(fun _ -> loadBitmap (imageViewModel.GetBytes()) imageViewModel.Size) with get,set
