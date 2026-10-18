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

    private async void OnNavigateToProfile(object sender, EventArgs e)
    {
        // PushAsync empile le profil par-dessus la carte. 
        // Ainsi, le bouton retour (PopAsync) du profil ramènera naturellement à la carte.
        await Navigation.PushAsync(new TutorProfilePage());
    }
}