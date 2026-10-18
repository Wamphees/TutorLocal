namespace TutorLocal;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnNavigateToLogin(object sender, EventArgs e)
{
    await Navigation.PushAsync(new LoginPage());
}
}
