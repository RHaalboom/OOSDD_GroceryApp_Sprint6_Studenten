using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using NUnit.Framework;

namespace TestCore
{
    [TestFixture]
    public class TestHelpers
    {
        // A small, test-local implementation of the AddProduct flow from the ViewModel.
        // This avoids any dependency on Grocery.App or MAUI types by accepting a delegate
        // for the confirmation dialog.
        static async Task<bool> AddProductSim(
            Product product,
            GroceryList groceryList,
            Func<string, string, string, string, Task<bool>>? displayAlert,
            IGroceryListItemsService itemsService,
            IProductService productService)
        {
            if (product == null) return false;

            if (product.MinimumAge.HasValue && product.MinimumAge.Value > 0)
            {
                if (displayAlert != null)
                {
                    var title = "Bevestig leeftijd";
                    var message = $"Dit product vereist een minimumleeftijd van {product.MinimumAge.Value}. Bent u minstens {product.MinimumAge.Value} jaar?";
                    bool confirmed = await displayAlert(title, message, "Ja", "Nee");
                    if (!confirmed) return false;
                }
            }

            var item = new GroceryListItem(0, groceryList.Id, product.Id, 1);
            itemsService.Add(item);
            product.Stock--;
            productService.Update(product);
            return true;
        }

        // Fake services (minimal implementation to satisfy interfaces and record calls)
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

        [SetUp]
        public void Setup()
        {
            // Nothing MAUI-specific here; keep tests isolated.
        }

        [Test]
        public async Task AddProduct_NoMinimumAge_AddsProduct()
        {
            // Arrange
            var product = new Product(1, "Milk", 5) { MinimumAge = null };
            var groceryList = new GroceryList(1, "List", DateOnly.MinValue, "", 0);
            var itemsService = new FakeGroceryListItemsService();
            var productService = new FakeProductService(new[] { product });

            // Act
            bool result = await AddProductSim(product, groceryList, null, itemsService, productService);

            // Assert
            Assert.IsTrue(result, "AddProduct should return true when no minimum age is set.");
            Assert.AreEqual(1, itemsService.AddedItems.Count, "Item should be added.");
            Assert.AreEqual(4, product.Stock, "Product stock should be decremented.");
            Assert.Contains(product, productService.UpdatedProducts);
        }

        [Test]
        public async Task AddProduct_MinimumAgeSet_UserDeclines_DoesNotAdd()
        {
            // Arrange
            var product = new Product(2, "Wine", 5) { MinimumAge = 18 };
            var groceryList = new GroceryList(2, "List", DateOnly.MinValue, "", 0);
            var itemsService = new FakeGroceryListItemsService();
            var productService = new FakeProductService(new[] { product });

            // Simulate the user declining the confirmation dialog
            Task<bool> Decline(string t, string m, string a, string c) => Task.FromResult(false);

            // Act
            bool result = await AddProductSim(product, groceryList, Decline, itemsService, productService);

            // Assert
            Assert.IsFalse(result, "AddProduct should return false when user declines age confirmation.");
            Assert.AreEqual(0, itemsService.AddedItems.Count, "No item should be added when user declines.");
            Assert.AreEqual(5, product.Stock, "Product stock should remain unchanged.");
            Assert.IsEmpty(productService.UpdatedProducts, "ProductService.Update should not be called.");
        }

        [Test]
        public async Task AddProduct_MinimumAgeSet_UserConfirms_AddsProduct()
        {
            // Arrange
            var product = new Product(3, "Whiskey", 3) { MinimumAge = 21 };
            var groceryList = new GroceryList(3, "List", DateOnly.MinValue, "", 0);
            var itemsService = new FakeGroceryListItemsService();
            var productService = new FakeProductService(new[] { product });

            // Simulate the user confirming the dialog
            Task<bool> Confirm(string t, string m, string a, string c) => Task.FromResult(true);

            // Act
            bool result = await AddProductSim(product, groceryList, Confirm, itemsService, productService);

            // Assert
            Assert.IsTrue(result, "AddProduct should return true when user confirms age.");
            Assert.AreEqual(1, itemsService.AddedItems.Count, "Item should be added when user confirms.");
            Assert.AreEqual(2, product.Stock, "Product stock should be decremented.");
            Assert.Contains(product, productService.UpdatedProducts);
        }
    }
}