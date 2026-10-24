using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class SyncInventoryService
    {
        private readonly JsonFileStore _store;

        public SyncInventoryService(JsonFileStore store) => _store = store;

        public bool ReserveStock(Order order)
        {
            Log.Info($"SyncInventoryService -> Order #{order.Id}: opening connection...");
            Thread.Sleep(200);
            Log.Info($"SyncInventoryService -> Order #{order.Id}: beginning transaction...");
            Thread.Sleep(100);
            Log.Info($"SyncInventoryService -> Order #{order.Id}: reserving stock...");
            Thread.Sleep(400);

            string? failure = null;

            _store.Update<Product>(DataFiles.Products, products =>
            {
                var needed = order.Items
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                    .ToList();

                foreach (var n in needed)
                {
                    var product = products.FirstOrDefault(p => p.Id == n.ProductId);
                    if (product is null) { failure = $"Product #{n.ProductId} not found"; return; }
                    if (product.StockQuantity < n.Quantity) { failure = $"Insufficient stock for '{product.Name}'"; return; }
                }

                foreach (var n in needed)
                    products.First(p => p.Id == n.ProductId).StockQuantity -= n.Quantity;
            });

            Log.Info($"SyncInventoryService -> Order #{order.Id}: {(failure is null ? "committing" : "rolling back")}...");
            Thread.Sleep(200);

            if (failure is not null)
            {
                order.FailureReason = failure;
                Log.Error($"SyncInventoryService -> Order #{order.Id}: nothing reserved ({failure})");
                return false;
            }

            Log.Success($"SyncInventoryService -> Order #{order.Id}: stock reserved");
            return true;
        }

        public void ReleaseStock(Order order)
        {
            Log.Info($"SyncInventoryService -> Order #{order.Id}: releasing stock...");
            Thread.Sleep(700);

            _store.Update<Product>(DataFiles.Products, products =>
            {
                foreach (var item in order.Items)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product is not null) product.StockQuantity += item.Quantity;
                }
            });

            Thread.Sleep(200);
            Log.Success($"SyncInventoryService -> Order #{order.Id}: stock released");
        }
    }
}
