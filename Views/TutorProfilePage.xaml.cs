namespace TutorLocal;

public partial class TutorProfilePage : ContentPage
{
    public TutorProfilePage()
    {
        InitializeComponent();
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}