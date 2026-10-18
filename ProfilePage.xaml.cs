namespace TutorLocal;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    private void OnNavigateToDashboard(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new DashboardPage());
    }

    private void OnNavigateToMap(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new MapPage());
    }

    private void OnNavigateToSessions(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new SessionsPage());
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        // Retourne à la page d'accueil principale pour simuler une déconnexion
        Application.Current.MainPage = new NavigationPage(new MainPage());
    }

    
}