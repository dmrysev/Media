namespace Media.Application.MAUI;

public partial class AppShell : Shell
{
	Action quit;

	public AppShell(Action quit)
	{
		InitializeComponent();
		this.quit = quit;
	}

	public void OnQuit_Clicked(object sender, EventArgs e) {
		quit();
	}
}
