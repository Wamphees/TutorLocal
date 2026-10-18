namespace TutorLocal;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    // Gère le clic sur le bouton "←" en haut à gauche
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        // PopAsync fait glisser la page hors de l'écran pour revenir à la précédente
        await Navigation.PopAsync();
    }

    // Gère le clic sur le lien "Créer un compte" en bas
    private async void OnNavigateToRegister(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}