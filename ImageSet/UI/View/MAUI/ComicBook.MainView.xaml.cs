namespace Media.UI.View.MAUI.ComicBook;

public partial class MainView : ContentView
{
	Media.UI.Core.ComicBook.MainViewModel ComicBook;

	public MainView (Media.UI.Core.ComicBook.MainViewModel comicBook) {
		InitializeComponent();
		ComicBook = comicBook;
		BindingContext = ComicBook;
		ComicBook.SetComicFileIndex(0);
	}

	void OnTapGestureRecognizerTapped(object sender, TappedEventArgs e) {
		Point pos = e.GetPosition(null).Value;
		var posCore = new Util.Drawing.Point(pos.X, pos.Y);
		ComicBook.OnScreenTapped (posCore);
	}
}

