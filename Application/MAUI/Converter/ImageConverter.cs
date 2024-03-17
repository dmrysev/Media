namespace Media.UI.Core.MAUI;

using System.Globalization;

public class ImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var imageBytes = (byte[]) value;
        var imageStream = new System.IO.MemoryStream(imageBytes);
        return ImageSource.FromStream(() => imageStream);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new System.NotImplementedException();
    }
}