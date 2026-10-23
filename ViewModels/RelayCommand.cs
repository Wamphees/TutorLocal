using System.Windows.Input;

namespace TutorLocal.ViewModels;

/// <summary>Commande synchrone avec état d'exécution.</summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : (Func<object?, bool>)(_ => canExecute()))
    {
    }
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Commande asynchrone : désactive automatiquement IsBusy pendant l'exécution.</summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Func<bool>? _isBusySource;

    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null, Func<bool>? isBusySource = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _isBusySource = isBusySource;
    }

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Func<bool>? isBusySource = null)
        : this(_ => execute(), ToCanExecute(canExecute), isBusySource)
    {
    }

    private static Func<object?, bool>? ToCanExecute(Func<bool>? canExecute) =>
        canExecute == null ? null : new Func<object?, bool>(_ => canExecute());

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isBusySource != null && _isBusySource())
            return false;
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        try
        {
            await _execute(parameter).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // Les ViewModels transforment l'exception en message utilisateur.
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
