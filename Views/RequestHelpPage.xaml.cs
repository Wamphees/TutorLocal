using TutorLocal.ViewModels;

namespace TutorLocal.Views;

public partial class RequestHelpPage : ContentPage
{
    private readonly RequestHelpViewModel _viewModel;

    public RequestHelpPage(RequestHelpViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
