using TutorLocal.ViewModels;

namespace TutorLocal.Views;

public partial class TutorProfilePage : ContentPage
{
    private readonly TutorProfileViewModel _viewModel;

    public TutorProfilePage(TutorProfileViewModel viewModel)
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
