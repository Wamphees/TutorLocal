using TutorLocal.ViewModels;

namespace TutorLocal.Views;

public partial class RatingPage : ContentPage
{
    private readonly RatingViewModel _viewModel;

    public RatingPage(RatingViewModel viewModel)
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
