using System;
using Grocery.Core.Data.Repositories;
using Grocery.Core.Models;
using NUnit.Framework;

namespace TestCore
{
    [TestFixture]
    public class TestHelpers
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void AddProduct_ShouldPersistAndBeQueryable()
        {
            // Arrange: use a unique name to avoid UNIQUE constraint collisions with seed data
            string uniqueName = $"test-product-{Guid.NewGuid():N}";
            var repo = new ProductRepository();
            var product = new Product(0, uniqueName, 5, DateOnly.FromDateTime(DateTime.Today.AddDays(30)), 1.23m);

            // Act
            Product created = repo.Add(product);

            try
            {
                // Assert: repository returned an id and item can be queried
                Assert.That(created, Is.Not.Null);
                Assert.That(created.Id, Is.GreaterThan(0), "Created product must have assigned Id.");

                Product? fetched = repo.Get(created.Id);
                Assert.That(fetched, Is.Not.Null, "Fetched product must not be null.");
                Assert.That(fetched!.Name, Is.EqualTo(uniqueName));
                Assert.That(fetched.Stock, Is.EqualTo(5));
                Assert.That(fetched.Price, Is.EqualTo(1.23m));
            }
            finally
            {
                // Clean up so repeated test runs remain deterministic
                repo.Delete(created);
            }
        }
    }
}