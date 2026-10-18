namespace TutorLocal;

public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();
    }

    // Gestion du clic sur l'icône "Accueil" dans la barre du bas
    private void OnNavigateToDashboard(object sender, EventArgs e)
    {
        // On remplace la page active par le Dashboard
        Application.Current.MainPage = new NavigationPage(new DashboardPage());
    }
}