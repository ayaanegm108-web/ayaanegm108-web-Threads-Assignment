using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class OrderRepository
    {
        private readonly JsonFileStore _store;

        public OrderRepository(JsonFileStore store) => _store = store;

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Executing query for all orders...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var orders = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);
            Log.Success($"OrderRepository -> {orders.Count} orders loaded");
            return orders;
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"OrderRepository -> Executing query for orders with status {status}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var all = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);
            var filtered = all.Where(o => o.Status == status).ToList();
            Log.Success($"OrderRepository -> {filtered.Count} orders with status {status}");
            return filtered;
        }
    }
}
