using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class SyncProductRepository
    {
        private readonly JsonFileStore _store;

        public SyncProductRepository(JsonFileStore store) => _store = store;

        public Product? GetById(int id)
        {
            Log.Info("ProductRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"ProductRepository -> Executing query for Product #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var products = _store.ReadAll<Product>(DataFiles.Products);
            var product = products.FirstOrDefault(p => p.Id == id);

            if (product is null)
                Log.Info($"ProductRepository -> Product #{id} not found");
            else
                Log.Success($"ProductRepository -> Product #{id} found ({product.Name}, stock {product.StockQuantity})");

            return product;
        }
    }
}
