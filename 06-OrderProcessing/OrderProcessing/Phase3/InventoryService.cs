using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class InventoryService
    {
        private readonly JsonFileStore _store;

        public InventoryService(JsonFileStore store) => _store = store;


        public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
        {
            Log.Info($"InventoryService -> Order #{order.Id}: opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"InventoryService -> Order #{order.Id}: beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"InventoryService -> Order #{order.Id}: reserving stock for {order.Items.Count} item(s)...");
            await Task.Delay(400, ct);

            string? failure = null;

            await _store.UpdateAsync<Product>(DataFiles.Products, products =>
            {

                var needed = order.Items
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                    .ToList();


                foreach (var n in needed)
                {
                    var product = products.FirstOrDefault(p => p.Id == n.ProductId);

                    if (product is null)
                    {
                        failure = $"Product #{n.ProductId} not found";
                        return;
                    }

                    if (product.StockQuantity < n.Quantity)
                    {
                        failure = $"Insufficient stock for '{product.Name}'";
                        return;
                    }
                }


                foreach (var n in needed)
                    products.First(p => p.Id == n.ProductId).StockQuantity -= n.Quantity;
            }, ct);

            Log.Info($"InventoryService -> Order #{order.Id}: {(failure is null ? "committing" : "rolling back")}...");
            try
            {
                await Task.Delay(200, ct);
            }
            catch (OperationCanceledException) when (failure is null)
            {
                // The stock was already taken in the file but the caller will never learn
                // that (it only sees the exception), so undo it here before re-throwing.
                await _store.UpdateAsync<Product>(DataFiles.Products, products =>
                {
                    foreach (var item in order.Items)
                    {
                        var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product is not null)
                            product.StockQuantity += item.Quantity;
                    }
                }, CancellationToken.None);
                Log.Error($"InventoryService -> Order #{order.Id}: cancelled during commit, reservation undone");
                throw;
            }

            if (failure is not null)
            {
                order.FailureReason = failure;
                Log.Error($"InventoryService -> Order #{order.Id}: nothing reserved ({failure})");
                return false;
            }

            Log.Success($"InventoryService -> Order #{order.Id}: stock reserved");
            return true;
        }

        public async Task ReleaseStockAsync(Order order, CancellationToken ct)
        {
            Log.Info($"InventoryService -> Order #{order.Id}: opening connection (release)...");
            await Task.Delay(200, ct);

            Log.Info($"InventoryService -> Order #{order.Id}: beginning transaction (release)...");
            await Task.Delay(100, ct);

            Log.Info($"InventoryService -> Order #{order.Id}: releasing stock...");
            await Task.Delay(400, ct);

            await _store.UpdateAsync<Product>(DataFiles.Products, products =>
            {
                foreach (var item in order.Items)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product is not null)
                        product.StockQuantity += item.Quantity;
                }
            }, ct);

            Log.Info($"InventoryService -> Order #{order.Id}: committing (release)...");
            await Task.Delay(200, ct);

            Log.Success($"InventoryService -> Order #{order.Id}: stock released");
        }
    }
}
