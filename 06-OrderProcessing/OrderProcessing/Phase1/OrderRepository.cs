using OrderProcessing.Shared.Exceptions;
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
        private readonly HashSet<int> _failSaveForOrderIds;

       
        public OrderRepository(JsonFileStore store, IEnumerable<int>? failSaveForOrderIds = null)
        {
            _store = store;
            _failSaveForOrderIds = new HashSet<int>(failSaveForOrderIds ?? Enumerable.Empty<int>());
        }

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

        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            bool commitWillFail = order.Status == OrderStatus.Completed
                                  && _failSaveForOrderIds.Contains(order.Id);

            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"OrderRepository -> Saving Order #{order.Id}...");
            await Task.Delay(400, ct);

            
            if (!commitWillFail)
            {
                await _store.UpdateAsync<Order>(DataFiles.Orders, orders =>
                {
                    int index = orders.FindIndex(o => o.Id == order.Id);
                    if (index >= 0)
                        orders[index] = order;   
                    else
                        orders.Add(order);       
                }, ct);
            }

            Log.Info("OrderRepository -> Committing...");
            await Task.Delay(200, ct);

            if (commitWillFail)
            {
                Log.Error($"OrderRepository -> Order #{order.Id}: commit failed, transaction rolled back");
                throw new DatabaseWriteException("Connection lost during commit");
            }

            Log.Success($"OrderRepository -> Order #{order.Id} saved ({order.Status})");
        }
    }
}
