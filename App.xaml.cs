using TutorLocal.Services;
using TutorLocal.Views;

namespace TutorLocal;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var auth = MauiProgram.Services.GetRequiredService<IAuthService>();

		// Restaure la session si « Se souvenir de moi » était actif,
		// sinon on démarre sur la page d'accueil publique.
		if (auth.TryRestoreSession())
		{
			var startPage = auth.CurrentUser?.Role == Models.UserRole.Tutor
				? (Page)MauiProgram.Services.GetRequiredService<TutorDashboardPage>()
				: MauiProgram.Services.GetRequiredService<DashboardPage>();
			MainPage = new NavigationPage(startPage);
		}
		else
			MainPage = new NavigationPage(MauiProgram.Services.GetRequiredService<MainPage>());
	}
}
