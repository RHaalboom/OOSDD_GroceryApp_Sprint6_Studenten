using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;

namespace Grocery.Core.Data.Repositories
{
    public class GroceryListItemsRepository : DatabaseConnection, IGroceryListItemsRepository
    {
        private readonly List<GroceryListItem> groceryListItems = new();

        public GroceryListItemsRepository()
        {
            CreateTable(@"CREATE TABLE IF NOT EXISTS GroceryListItem (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [GroceryListId] INTEGER NOT NULL,
                            [ProductId] INTEGER NOT NULL,
                            [Amount] INTEGER NOT NULL)");

            // Ensure combination (GroceryListId, ProductId) is unique to avoid accidental duplicate rows
            CreateTable(@"CREATE UNIQUE INDEX IF NOT EXISTS IX_GroceryListItem_GroceryListId_ProductId 
                          ON GroceryListItem(GroceryListId, ProductId);");

            List<string> insertQueries = new()
            {
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 1, 3)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 2, 1)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 3, 4)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(2, 1, 2)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(2, 2, 5)"
            };
            InsertMultipleWithTransaction(insertQueries);
            GetAll();
        }

        public List<GroceryListItem> GetAll()
        {
            groceryListItems.Clear();
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    groceryListItems.Add(new GroceryListItem(id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return groceryListItems;
        }

        public List<GroceryListItem> GetAllOnGroceryListId(int id)
        {
            var result = new List<GroceryListItem>();
            string selectQuery = $"SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE GroceryListId = {id}";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    result.Add(new GroceryListItem(itemId, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return result;
        }

        public GroceryListItem Add(GroceryListItem item)
        {
            string insertQuery = "INSERT INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(@GroceryListId, @ProductId, @Amount) Returning RowId;";
            OpenConnection();
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);
                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            CloseConnection();
            return item;
        }

        public GroceryListItem? Delete(GroceryListItem item)
        {
            string deleteQuery = $"DELETE FROM GroceryListItem WHERE Id = {item.Id};";
            OpenConnection();
            //Connection.ExecuteNonQuery(deleteQuery);
            CloseConnection();
            return item;
        }

        public GroceryListItem? Get(int id)
        {
            GroceryListItem? listItem = null;
            string selectQuery = $"SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE Id = {id}";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    listItem = new GroceryListItem(itemId, groceryListId, productId, amount);
                }
            }
            CloseConnection();
            return listItem;
        }

        public GroceryListItem? Update(GroceryListItem item)
        {
            string updateQuery = $"UPDATE GroceryListItem SET GroceryListId = @GroceryListId, ProductId = @ProductId, Amount = @Amount WHERE Id = {item.Id};";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);
                command.ExecuteNonQuery();
            }
            CloseConnection();
            return item;
        }
    }
}
