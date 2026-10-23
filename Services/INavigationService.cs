using Microsoft.Extensions.DependencyInjection;

namespace TutorLocal.Services;

/// <summary>
/// Encapsule la navigation de l'application de façon découplée des ViewModels.
/// Les vues sont résolues via l'injection de dépendances.
/// </summary>
public interface INavigationService
{
    /// <summary>Empile une vue par-dessus la pile de navigation.</summary>
    Task PushAsync<TView>() where TView : Page;

    /// <summary>Revient à la vue précédente.</summary>
    Task PopAsync();

    /// <summary>Remplace la page racine (changement d'onglet bas de page ou de contexte).</summary>
    void SwitchTab<TView>() where TView : Page;
}

/// <inheritdoc />
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task PushAsync<TView>() where TView : Page
    {
        var view = _services.GetRequiredService<TView>();
        var page = Application.Current?.MainPage;
        if (page?.Navigation != null)
            await page.Navigation.PushAsync(view);
    }

    public async Task PopAsync()
    {
        var page = Application.Current?.MainPage;
        if (page?.Navigation?.NavigationStack?.Count > 1)
            await page.Navigation.PopAsync();
    }

    public void SwitchTab<TView>() where TView : Page
    {
        var view = _services.GetRequiredService<TView>();
        var current = Application.Current;
        if (current?.Dispatcher == null)
        {
            return;
        }
        // La barre de navigation basse change la racine de l'application :
        // l'utilisateur ne doit pas pouvoir "revenir" vers l'ancien onglet via le bouton système.
        current.Dispatcher.Dispatch(() =>
        {
            if (current != null)
                current.MainPage = new NavigationPage(view);
        });
    }
}
