using System.Collections.Specialized;
using TutorLocal.ViewModels;

namespace TutorLocal.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();

        if (_viewModel.Messages.Count > 0)
            ScrollToLastMessage();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Concerne la vue : défiler la conversation à chaque nouveau message.
        _viewModel.Messages.CollectionChanged -= OnMessagesChanged;
        _viewModel.Messages.CollectionChanged += OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScrollToLastMessage();
    }

    private void ScrollToLastMessage()
    {
        if (_viewModel.Messages.Count == 0 || ChatScrollView == null)
            return;

        // Petit délai : laisse la layout mesurer le nouveau message avant le défilement.
        Dispatcher?.DispatchDelayed(TimeSpan.FromMilliseconds(120), () =>
            ChatScrollView.ScrollToAsync(0, ChatScrollView.ContentSize.Height, true).FireAndForget());
    }
}

public static class TaskExtensions
{
    /// <summary>Évite que des exceptions de défilement fassent planter l'application.</summary>
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                System.Diagnostics.Debug.WriteLine(t.Exception);
        });
    }
}
