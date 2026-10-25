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
    private readonly Dictionary<Type, NavigationPage> _navigationPages = new();

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task PushAsync<TView>() where TView : Page
    {
        var view = _services.GetRequiredService<TView>();
        var page = Application.Current?.MainPage;
        if (page?.Navigation != null)
            await page.Navigation.PushAsync(view, animated: false);
    }

    public async Task PopAsync()
    {
        var page = Application.Current?.MainPage;
        if (page?.Navigation?.NavigationStack?.Count > 1)
            await page.Navigation.PopAsync(animated: false);
    }

    public void SwitchTab<TView>() where TView : Page
    {
        var current = Application.Current;
        if (current?.Dispatcher == null)
        {
            return;
        }

        current.Dispatcher.Dispatch(() =>
        {
            if (current.MainPage is not NavigationPage currentNavigation)
                return;

            var currentRoot = currentNavigation.RootPage;
            _navigationPages.TryAdd(currentRoot.GetType(), currentNavigation);

            if (!_navigationPages.TryGetValue(typeof(TView), out var targetNavigation))
            {
                targetNavigation = new NavigationPage(_services.GetRequiredService<TView>());
                _navigationPages.Add(typeof(TView), targetNavigation);
            }

            if (ReferenceEquals(currentNavigation, targetNavigation))
            {
                if (currentNavigation.Navigation.NavigationStack.Count > 1)
                    _ = currentNavigation.Navigation.PopToRootAsync(animated: false);
                return;
            }

            // Chaque onglet garde sa pile, mais le bouton système ne repasse pas
            // par les onglets précédemment visités.
            current.MainPage = targetNavigation;
        });
    }
}
