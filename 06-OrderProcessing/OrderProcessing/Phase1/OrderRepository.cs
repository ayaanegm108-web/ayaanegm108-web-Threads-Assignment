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
        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"OrderRepository -> Saving Order #{order.Id}...");
            await Task.Delay(400, ct);

            // الكتابة الفعلية: بعد delay الـ Insert/Update وقبل الـ Commit
            await _store.UpdateAsync<Order>(DataFiles.Orders, orders =>
            {
                int index = orders.FindIndex(o => o.Id == order.Id);
                if (index >= 0)
                    orders[index] = order;   // نفس الـ Id: استبدل
                else
                    orders.Add(order);       // جديد: ضيف
            }, ct);

            Log.Info("OrderRepository -> Committing...");
            await Task.Delay(200, ct);

            Log.Success($"OrderRepository -> Order #{order.Id} saved ({order.Status})");
        }
    }
}
