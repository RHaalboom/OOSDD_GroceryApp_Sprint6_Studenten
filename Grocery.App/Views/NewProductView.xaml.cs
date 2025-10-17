using System;
using Microsoft.Maui.Controls;
using Grocery.Core.Models;
using Grocery.App.ViewModels;
using Grocery.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Grocery.App.Views
{
    public partial class NewProductView : ContentPage
    {
        private readonly NewProductViewModel _viewModel;
        private readonly IProductService _productService;

        public NewProductView(NewProductViewModel viewModel, IProductService productService)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
    
            ShelfLifePicker.Date = DateTime.Today;

            // Handle create requests from the ViewModel: persist and broadcast the new product.
            _viewModel.CreateRequested += OnCreateRequested;
        }

        private async void OnCreateClicked(object? sender, EventArgs e)
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Error!", "Naam is verplicht", "OK");
                return;
            }

            if (!int.TryParse(StockEntry.Text, out int stock))
            {
                await DisplayAlert("Error!", "Stock moet een volledig nummer zijn", "OK");
                return;
            }

            if (!decimal.TryParse(PriceEntry.Text, out decimal price))
            {
                await DisplayAlert("Error!", "Prijs moet worden ingevuld", "OK");
                return;
            }

            DateOnly shelfLife = DateOnly.FromDateTime(ShelfLifePicker.Date);

            var product = new Product(0, name, stock, shelfLife, price);

            var cmd = _viewModel.CreateProductCommand;
            if (cmd != null && cmd.CanExecute(product))
            {
                cmd.Execute(product);

                NameEntry.Text = string.Empty;
                PriceEntry.Text = string.Empty;
                StockEntry.Text = string.Empty;
                ShelfLifePicker.Date = DateTime.Today;

                await DisplayAlert("Success", "Product aangemaakt!", "OK");
            }
            else
            {
                await DisplayAlert("Geen toegang.", "Alleen Admins kunnen producten toevoegen.", "OK");
            }
        }

        // Persist the product and notify other view models so UI updates immediately.
        private void OnCreateRequested(Product product)
        {
            if (product == null) return;

            // Persist using the shared product service (returns the created product with assigned Id).
            Product created = _productService.Add(product);

            // Broadcast to any interested view models (ProductViewModel registers for this).
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Product>(created));
        }
    }
}
