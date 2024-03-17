namespace Media.UI.View.MAUI.CommandPrompt;

public partial class MainView : ContentView
{
	public MainView (Media.UI.Core.CommandPrompt.MainViewModel viewModel) {
		InitializeComponent();
		BindingContext = viewModel;
	}
}

