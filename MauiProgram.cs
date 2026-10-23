using Microsoft.Extensions.Logging;
using TutorLocal.Services;
using TutorLocal.ViewModels;
using TutorLocal.Views;

namespace TutorLocal;

public static class MauiProgram
{
	/// <summary>Accès global au conteneur de services (nécessaire avant l'attachement du handler).</summary>
	public static IServiceProvider Services { get; private set; } = default!;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// ===== Architecture MVVM : services métier ================================
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton<ITutorService, TutorService>();
		builder.Services.AddSingleton<ISessionService, SessionService>();
		builder.Services.AddSingleton<IHelpRequestService, HelpRequestService>();
		builder.Services.AddSingleton<IChatService, ChatService>();

		// ===== ViewModels =========================================================
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<SessionsViewModel>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<RequestHelpViewModel>();
		builder.Services.AddTransient<RatingViewModel>();
		builder.Services.AddTransient<TutorProfileViewModel>();
		builder.Services.AddTransient<MapViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();

		// ===== Vues ===============================================================
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<SessionsPage>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<RequestHelpPage>();
		builder.Services.AddTransient<RatingPage>();
		builder.Services.AddTransient<TutorProfilePage>();
		builder.Services.AddTransient<MapPage>();
		builder.Services.AddTransient<ProfilePage>();


#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		Services = app.Services;
		return app;
	}
}
