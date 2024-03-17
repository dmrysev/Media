using System.Reactive.Linq;
using Java.Lang;

namespace Media.Application.MAUI;

public partial class MainPage : ContentPage
{
	public static API.Application.Dependency Dependency;
	public static System.Reactive.Subjects.Subject<int> Finalize = new();
	UI.Core.MainWindow.MainViewModel MainViewModel;

	public MainPage() {
		InitializeComponent();
		var getMainViewSize = () => new Util.Drawing.Size (Width, Height);
		var getMainViewSizeFSharp = Microsoft.FSharp.Core.FuncConvert.FromFunc (getMainViewSize);
    	var mainViewModel = new UI.Core.MainWindow.MainViewModel (Dependency, getMainViewSizeFSharp, MainPage.Finalize.AsObservable());
		MainViewModel = mainViewModel;
		BindingContext = mainViewModel;
		mainViewModel.CurrentMainContent = UI.Core.MainWindow.MainContent.ComicBook;
		CommandPrompt.Content = new Media.UI.View.MAUI.CommandPrompt.MainView (mainViewModel.CommandPrompt);
		mainViewModel.IsCommandPromptVisible = false;
		MainView.Content = mainViewModel.CurrentMainContent.Tag switch {
			UI.Core.MainWindow.MainContent.Tags.ComicBook => new Media.UI.View.MAUI.ComicBook.MainView ((Media.UI.Core.ComicBook.MainViewModel) mainViewModel.MainContent),
			_ => new Media.UI.View.MAUI.ComicBook.MainView (mainViewModel.ComicBook)
		};
	}
}

