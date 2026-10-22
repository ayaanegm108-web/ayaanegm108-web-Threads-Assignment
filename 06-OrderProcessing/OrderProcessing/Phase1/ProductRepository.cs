using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class ProductRepository
    {
        private readonly JsonFileStore _store;

        public ProductRepository(JsonFileStore store) => _store = store;

        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
        {
            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"ProductRepository -> Executing query for Product #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
            var product = products.FirstOrDefault(p => p.Id == id);

            if (product is null)
                Log.Info($"ProductRepository -> Product #{id} not found");
            else
                Log.Success($"ProductRepository -> Product #{id} found ({product.Name}, stock {product.StockQuantity})");

            return product;
        }

        
    }
}
