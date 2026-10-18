namespace TutorLocal;

public partial class RatingPage : ContentPage
{
    public RatingPage()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        // On ferme la page d'évaluation pour retourner à la page précédente
        await Navigation.PopAsync();
    }

    
}