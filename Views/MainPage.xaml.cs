// namespace TutorLocal;

// public partial class MainPage : ContentPage
// {
// 	int count = 0;

// 	public MainPage()
// 	{
// 		InitializeComponent();
// 	}

// 	private void OnCounterClicked(object sender, EventArgs e)
// 	{
// 		count++;

// 		if (count == 1)
// 			CounterBtn.Text = $"Clicked {count} time";
// 		else
// 			CounterBtn.Text = $"Clicked {count} times";

// 		SemanticScreenReader.Announce(CounterBtn.Text);
// 	}
// }

namespace TutorLocal;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    //methode  de navigation vers la page d'inscription
    private async void OnNavigateToRegister(object sender, EventArgs e)
    {
        // PushAsync fait glisser la nouvelle page par-dessus l'ancienne
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnNavigateToLogin(object sender, EventArgs e)
{
    await Navigation.PushAsync(new LoginPage());
}
}