using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grocery.App.ViewModels;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using NUnit.Framework;


namespace Microsoft.Maui.Controls
{
    public class Application
    {
        public static Application? Current { get; set; }
        public Page? MainPage { get; set; }
    }

    public class Page
    {
        public virtual Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
            => Task.FromResult(true);
    }

    public class ContentPage : Page { }
}

namespace TestCore
{
    [TestFixture]
    public class TestHelpers
    {
        [SetUp]
        public void Setup()
        {
            // Ensure clean Application.Current for tests that rely on MainPage
            Application.Current = new Microsoft.Maui.Controls.Application();
        }

        // A small test page that controls the confirmation response.
        class TestPage : Microsoft.Maui.Controls.ContentPage
        {
            public bool Response { get; set; }
            public override Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
                => Task.FromResult(Response);
        }

        // Minimal fakes for services — these implement your real application interfaces
        // and let the actual ViewModel logic run.
        class FakeGroceryListItemsService : IGroceryListItemsService
        {
            public List<GroceryListItem> AddedItems { get; } = new();
            public List<GroceryListItem> ItemsOnList { get; set; } = new();

            public GroceryListItem Add(GroceryListItem item)
            {
                AddedItems.Add(item);
                return item;
            }

            public GroceryListItem? Delete(GroceryListItem item) => null;
            public GroceryListItem? Get(int id) => AddedItems.FirstOrDefault(x => x.Id == id);
            public List<GroceryListItem> GetAll() => AddedItems;
            public List<GroceryListItem> GetAllOnGroceryListId(int groceryListId) => ItemsOnList.Where(i => i.GroceryListId == groceryListId).ToList();
            public GroceryListItem? Update(GroceryListItem item)
            {
                var idx = AddedItems.FindIndex(x => x.Id == item.Id);
                if (idx >= 0) AddedItems[idx] = item;
                return item;
            }

            public List<BestSellingProducts> GetBestSellingProducts(int topX = 5) => new();
        }

        class FakeProductService : IProductService
        {
            readonly List<Product> _products;
            public List<Product> UpdatedProducts { get; } = new();

            public FakeProductService(IEnumerable<Product> products) => _products = products.ToList();

            public List<Product> GetAll() => _products;
            public Product Add(Product item) { _products.Add(item); return item; }
            public Product? Delete(Product item) { _products.Remove(item); return item; }
            public Product? Get(int id) => _products.FirstOrDefault(p => p.Id == id);
            public Product? Update(Product item)
            {
                UpdatedProducts.Add(item);
                return item;
            }
        }

        class FakeFileSaverService : IFileSaverService
        {
            public Task SaveFileAsync(string fileName, string content, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Test]
        public async Task AddProduct_MinimumAgeSet_UserDeclines_NoAdd_RealViewModel()
        {
            // Arrange - use the real ViewModel from Grocery.App
            var product = new Product(10, "Wine", 5) { MinimumAge = 18 };
            var itemsService = new FakeGroceryListItemsService();
            var productService = new FakeProductService(new[] { product });
            var fileSaver = new FakeFileSaverService();

            // Provide a TestPage that returns 'false' for confirmation
            var page = new TestPage { Response = false };
            Application.Current!.MainPage = page;

            var vm = new GroceryListItemsViewModel(itemsService, productService, fileSaver)
            {
                GroceryList = new GroceryList(1, "Test", DateOnly.MinValue, "", 0)
            };

            // Act
            await vm.AddProduct(product);

            Assert.AreEqual(0, itemsService.AddedItems.Count, "Item should NOT be added when user declines the confirmation.");
            Assert.IsEmpty(productService.UpdatedProducts, "ProductService.Update should not be called.");
        }

        [Test]
        public async Task AddProduct_MinimumAgeSet_UserConfirms_Adds_RealViewModel()
        {
            // Arrange - use the real ViewModel from Grocery.App
            var product = new Product(11, "Whiskey", 3) { MinimumAge = 21 };
            var itemsService = new FakeGroceryListItemsService();
            var productService = new FakeProductService(new[] { product });
            var fileSaver = new FakeFileSaverService();

            // Provide a TestPage that returns 'true' for confirmation
            var page = new TestPage { Response = true };
            Application.Current!.MainPage = page;

            var vm = new GroceryListItemsViewModel(itemsService, productService, fileSaver)
            {
                GroceryList = new GroceryList(2, "Test2", DateOnly.MinValue, "", 0)
            };

            // Act
            await vm.AddProduct(product);

            Assert.AreEqual(1, itemsService.AddedItems.Count, "Item should be added when user confirms.");
            Assert.Contains(product, productService.UpdatedProducts);
        }

        [Test]
        public async Task AddProduct_NoMinimumAge_Adds_RealViewModel()
        {
            // Arrange
            var product = new Product(12, "Jam", 5) { MinimumAge = null };
            var itemsService = new FakeGroceryListItemsService();
            var productService = new FakeProductService(new[] { product });
            var fileSaver = new FakeFileSaverService();

            // No MainPage needed because MinimumAge is null; but ensure Application is set
            Application.Current!.MainPage = null;

            var vm = new GroceryListItemsViewModel(itemsService, productService, fileSaver)
            {
                GroceryList = new GroceryList(3, "Test3", DateOnly.MinValue, "", 0)
            };

            // Act
            await vm.AddProduct(product);

            // Assert
            Assert.AreEqual(1, itemsService.AddedItems.Count, "Item should be added when no minimum age is set.");
            Assert.Contains(product, productService.UpdatedProducts);
        }
    }
}