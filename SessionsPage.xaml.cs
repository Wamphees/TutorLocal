namespace TutorLocal;

public partial class SessionsPage : ContentPage
{
    public SessionsPage()
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
}