using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        public ObservableCollection<Product> Products { get; set; }

        public ProductViewModel(IProductService productService)
        {
            _productService = productService;
            Products = new ObservableCollection<Product>();
            foreach (Product p in _productService.GetAll()) Products.Add(p);
        }

        // Ensure the product list is refreshed whenever the view appears.
        public override void OnAppearing()
        {
            base.OnAppearing();
            Refresh();
        }

        // Public refresh method so other code can force a reload if needed.
        public void Refresh()
        {
            Products.Clear();
            foreach (Product p in _productService.GetAll()) Products.Add(p);
        }
    }
}
