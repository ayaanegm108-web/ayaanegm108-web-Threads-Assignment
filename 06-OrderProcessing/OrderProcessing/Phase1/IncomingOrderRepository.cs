using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class IncomingOrderRepository
    {
        private readonly JsonFileStore _store;

        public IncomingOrderRepository(JsonFileStore store) => _store = store;

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("IncomingOrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("IncomingOrderRepository -> Executing query for all incoming orders...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var orders = await _store.ReadAllAsync<Order>(DataFiles.IncomingOrders, ct);
            Log.Success($"IncomingOrderRepository -> {orders.Count} incoming orders loaded");
            return orders;
        }
    }
}
