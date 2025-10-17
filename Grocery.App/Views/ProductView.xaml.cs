using Grocery.App.ViewModels;
using Microsoft.Maui.Controls;

namespace Grocery.App.Views;

public partial class ProductView : ContentPage
{
	private readonly ProductViewModel _viewModel;

	public ProductView(ProductViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Tell the VM to refresh so newly added products show immediately.
        _viewModel?.OnAppearing();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        // Navigate to the NewProductView using the registered route.
        // NewProductView was registered in DI and with Routing.RegisterRoute in MauiProgram.
        await Shell.Current.GoToAsync(nameof(NewProductView));
    }
}