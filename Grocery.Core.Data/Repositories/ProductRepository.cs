using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Grocery.Core.Data.Helpers;

namespace Grocery.Core.Data.Repositories
{
    public class ProductRepository : DatabaseConnection, IProductRepository
    {
        private readonly List<Product> products = new();

        public ProductRepository()
        {
            CreateTable(@"CREATE TABLE IF NOT EXISTS Product (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [Name] NVARCHAR(120) UNIQUE NOT NULL,
                            [Stock] INTEGER NOT NULL,
                            [ShelfLife] DATE,
                            [Price] NUMERIC NOT NULL DEFAULT 0,
                            [MinimumAge] INTEGER NULL)");

            List<string> insertQueries = new()
            {
                // sample data; uses ISO date format. INSERT OR IGNORE to avoid duplicates.
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price, MinimumAge) VALUES('Melk', 300, '2025-09-25', 0.95, NULL)",
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price, MinimumAge) VALUES('Kaas', 100, '2025-09-30', 7.98, NULL)",
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price, MinimumAge) VALUES('Brood', 400, '2025-09-12', 2.19, NULL)",
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price, MinimumAge) VALUES('Cornflakes', 0, '2025-12-31', 1.48, NULL)"
            };

            // Only insert the sample data when the Product table is empty (initial startup).
            int existingCount = 0;
            OpenConnection();
            using (var countCmd = new SqliteCommand("SELECT COUNT(1) FROM Product;", Connection))
            {
                var result = countCmd.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int parsed))
                {
                    existingCount = parsed;
                }
            }
            CloseConnection();

            if (existingCount == 0)
            {
                InsertMultipleWithTransaction(insertQueries);
            }

            GetAll();
        }

        public List<Product> GetAll()
        {
            products.Clear();
            string selectQuery = "SELECT Id, Name, Stock, ShelfLife, Price, MinimumAge FROM Product";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);

                    DateOnly shelfLife = default;
                    if (!reader.IsDBNull(3))
                    {
                        // SQLite returns date/time as text -> GetDateTime works if stored as ISO date
                        var dt = reader.GetDateTime(3);
                        shelfLife = DateOnly.FromDateTime(dt);
                    }

                    decimal price = 0m;
                    if (!reader.IsDBNull(4))
                    {
                        // Use Convert to handle numeric stored as double/decimal
                        price = Convert.ToDecimal(reader.GetValue(4));
                    }

                    int? minimumAge = null;
                    if (!reader.IsDBNull(5))
                    {
                        minimumAge = reader.GetInt32(5);
                    }

                    products.Add(new Product(id, name, stock, shelfLife, price, minimumAge));
                }
            }
            CloseConnection();
            return products;
        }

        public Product? Get(int id)
        {
            Product? product = null;
            string selectQuery = $"SELECT Id, Name, Stock, ShelfLife, Price, MinimumAge FROM Product WHERE Id = {id}";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int pid = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);

                    DateOnly shelfLife = default;
                    if (!reader.IsDBNull(3))
                    {
                        shelfLife = DateOnly.FromDateTime(reader.GetDateTime(3));
                    }

                    decimal price = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4));

                    int? minimumAge = null;
                    if (!reader.IsDBNull(5))
                    {
                        minimumAge = reader.GetInt32(5);
                    }

                    product = new Product(pid, name, stock, shelfLife, price, minimumAge);
                }
            }
            CloseConnection();
            return product;
        }

        public Product Add(Product item)
        {
            string insertQuery = @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price, MinimumAge)
                                   VALUES(@Name, @Stock, @ShelfLife, @Price, @MinimumAge);
                                   SELECT Id FROM Product WHERE Name = @Name;";

            OpenConnection();
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Stock", item.Stock);

                if (item.ShelfLife == default)
                    command.Parameters.AddWithValue("@ShelfLife", DBNull.Value);
                else
                    command.Parameters.AddWithValue("@ShelfLife", item.ShelfLife);

                command.Parameters.AddWithValue("@Price", item.Price);

                if (item.MinimumAge.HasValue)
                    command.Parameters.AddWithValue("@MinimumAge", item.MinimumAge.Value);
                else
                    command.Parameters.AddWithValue("@MinimumAge", DBNull.Value);

                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    item.Id = id;
                }
            }
            CloseConnection();
            return item;
        }

        public Product? Delete(Product item)
        {
            string deleteQuery = $"DELETE FROM Product WHERE Id = {item.Id};";
            OpenConnection();
            using (var cmd = new SqliteCommand(deleteQuery, Connection))
            {
                cmd.ExecuteNonQuery();
            }
            CloseConnection();
            return item;
        }

        public Product? Update(Product item)
        {
            string updateQuery = $"UPDATE Product SET Name = @Name, Stock = @Stock, ShelfLife = @ShelfLife, Price = @Price, MinimumAge = @MinimumAge WHERE Id = {item.Id};";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Stock", item.Stock);

                if (item.ShelfLife == default)
                    command.Parameters.AddWithValue("@ShelfLife", DBNull.Value);
                else
                    command.Parameters.AddWithValue("@ShelfLife", item.ShelfLife);

                command.Parameters.AddWithValue("@Price", item.Price);

                if (item.MinimumAge.HasValue)
                    command.Parameters.AddWithValue("@MinimumAge", item.MinimumAge.Value);
                else
                    command.Parameters.AddWithValue("@MinimumAge", DBNull.Value);

                command.ExecuteNonQuery();
            }
            CloseConnection();
            return item;
        }
    }
}
