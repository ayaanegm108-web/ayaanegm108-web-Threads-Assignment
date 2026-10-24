using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class SyncOrderRepository
    {
        private readonly JsonFileStore _store;

        public SyncOrderRepository(JsonFileStore store) => _store = store;

        public void Save(Order order)
        {
            Log.Info("SyncOrderRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info("SyncOrderRepository -> Beginning transaction...");
            Thread.Sleep(100);

            Log.Info($"SyncOrderRepository -> Saving Order #{order.Id}...");
            Thread.Sleep(400);

            _store.Update<Order>(DataFiles.Orders, orders =>
            {
                int index = orders.FindIndex(o => o.Id == order.Id);
                if (index >= 0) orders[index] = order;
                else orders.Add(order);
            });

            Log.Info("SyncOrderRepository -> Committing...");
            Thread.Sleep(200);

            Log.Success($"SyncOrderRepository -> Order #{order.Id} saved ({order.Status})");
        }
    }
}
