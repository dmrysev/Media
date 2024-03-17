namespace Media.Application.MAUI;

public partial class App : Microsoft.Maui.Controls.Application
{
	CoreApplication CoreApp = new();

	public App() {
		InitializeComponent();
		var quit = () => { 
			((System.IDisposable) CoreApp).Dispose();
			this.Quit(); };
		MainPage = new AppShell (quit);
		Application.MAUI.MainPage.Dependency = CoreApp.Dependency;
	}

	protected override Window CreateWindow(IActivationState activationState) {
		Window window = base.CreateWindow(activationState);
		window.Stopped += (s, e) => {
			((System.IDisposable) CoreApp).Dispose();
			Application.MAUI.MainPage.Finalize.OnNext(0);
		};
		return window;
	}	
}
