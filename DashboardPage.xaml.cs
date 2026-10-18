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
}