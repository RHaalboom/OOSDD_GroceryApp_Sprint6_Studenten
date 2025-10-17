using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Grocery.Core.Models;
using Grocery.Core.Interfaces.Services;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    public class NewProductViewModel : BaseViewModel
    {
        private readonly GlobalViewModel _globalViewModel;
        private readonly IProductService _productService;

        public ICommand CreateProductCommand { get; }

        public event Action<Product>? CreateRequested;

        public NewProductViewModel(GlobalViewModel globalViewModel, IProductService productService)
        {
            _globalViewModel = globalViewModel ?? throw new ArgumentNullException(nameof(globalViewModel));
            _productService = productService;

            CreateProductCommand = new Command<Product>(async p => await ExecuteCreateProductAsync(p), p => CanCreateProduct);

            if (_globalViewModel is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(GlobalViewModel.Client) ||
                        e.PropertyName == "Client") // defensive
                    {
                        if (CreateProductCommand is Command cmd)
                            cmd.ChangeCanExecute();
                    }
                };
            }

            Products = new ObservableCollection<Product>();
            foreach (Product p in _productService.GetAll()) Products.Add(p);
        }

        public ObservableCollection<Product> Products { get; }

        public bool CanCreateProduct => _globalViewModel.Client?.Role == Role.Admin;

        private async Task ExecuteCreateProductAsync(Product product)
        {
            if (!CanCreateProduct)
            {
                await Shell.Current.DisplayAlert("Permission denied", "Only administrators can create products.", "OK");
                return;
            }

            CreateRequested?.Invoke(product);
        }
    }
}