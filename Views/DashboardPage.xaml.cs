namespace TutorLocal;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }
    private void OnNavigateToMap(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new MapPage());
    }

    private async void OnNavigateToRequestHelp(object sender, EventArgs e)
    {
        // On empile la page de demande par-dessus le tableau de bord
        await Navigation.PushAsync(new RequestHelpPage());
    }

    private void OnNavigateToSessions(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new SessionsPage());
    }

    private async void OnNavigateToChat(object sender, EventArgs e)
    {
        // Empile la page de chat par-dessus le tableau de bord
        await Navigation.PushAsync(new ChatPage());
    }
    private void OnNavigateToProfile(object sender, EventArgs e)
{
    Application.Current.MainPage = new NavigationPage(new ProfilePage());
}
}