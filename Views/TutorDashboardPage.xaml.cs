using TutorLocal.ViewModels;

namespace TutorLocal.Views;

public partial class TutorDashboardPage : ContentPage
{
    private readonly TutorDashboardViewModel _viewModel;

    public TutorDashboardPage(TutorDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
