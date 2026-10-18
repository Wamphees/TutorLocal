namespace TutorLocal;

public partial class RequestHelpPage : ContentPage
{
    public RequestHelpPage()
    {
        InitializeComponent();
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}