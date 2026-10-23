using System.Windows.Input;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>
/// Base de tous les ViewModels de page : accès au service de navigation
/// et utilitaires communs (état, alertes, observable collection).
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    protected ViewModelBase(INavigationService navigation)
    {
        Navigation = navigation;
    }

    protected INavigationService Navigation { get; }

    protected IDispatcher? Dispatcher => Application.Current?.Dispatcher;

    /// <summary>Se déclenche à l'apparition de la vue (rechargement des données).</summary>
    public virtual void OnAppearing()
    {
    }

    protected Task AlertAsync(string title, string? message = null, string? cancel = null) =>
        Application.Current?.MainPage?.DisplayAlert(title, string.IsNullOrWhiteSpace(message) ? null : message, cancel ?? "OK")
            ?? Task.CompletedTask;

    /// <summary>Commande de navigation « retour ».</summary>
    public ICommand BackCommand => new AsyncRelayCommand(async () => await Navigation.PopAsync(), isBusySource: () => IsBusy);

    protected static Task ScrollToEnd(ScrollView? scrollView, View? target)
    {
        return scrollView != null && target != null
            ? scrollView.ScrollToAsync(target, ScrollToPosition.End, true)
            : Task.CompletedTask;
    }
}
