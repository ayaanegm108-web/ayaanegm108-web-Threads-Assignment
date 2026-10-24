using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase3
{
    public static class SyncVsAsyncComparison
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            var orderRepo = new OrderRepository(store);
            var incoming = new IncomingOrderRepository(store);

            Console.WriteLine("\n===== Task 9.6: sync (10 threads) vs async (10 tasks) - orders #1-#10, no failures =====");

            // ---------- SYNC ----------
            await DataSeeder.ResetAsync(store, ct);
            var syncOrders = (await incoming.GetAllAsync(ct)).Where(o => o.Id <= 10).ToList();

            var customersSync = new SyncCustomerRepository(store);
            var syncProcessor = new SyncOrderProcessor(
                customersSync,
                new SyncInventoryService(store),
                new SyncPricingService(store, customersSync, new SyncCouponRepository(store)),
                new SyncPaymentGateway(store),
                new SyncOrderRepository(store),
                new SyncEmailService(store));

            Console.WriteLine("\n--- Sync run: one Thread per order ---");
            Log.ResetThreadTracking();
            var swSync = Stopwatch.StartNew();

            var threads = new List<Thread>();
            foreach (var order in syncOrders)
            {
                var o = order;
                var t = new Thread(() => syncProcessor.ProcessOrder(o));
                threads.Add(t);
            }
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            swSync.Stop();
            int syncThreads = Log.DistinctThreads;
            int syncPool = ThreadPool.ThreadCount;
            var syncResult = await orderRepo.GetAllAsync(ct);

            // ---------- ASYNC ----------
            await DataSeeder.ResetAsync(store, ct);
            var asyncOrders = (await incoming.GetAllAsync(ct)).Where(o => o.Id <= 10).ToList();
            var asyncProcessor = Phase3Runner.BuildProcessor(store, withFailures: false);

            Console.WriteLine("\n--- Async run: one Task per order ---");
            Log.ResetThreadTracking();
            var swAsync = Stopwatch.StartNew();

            var tasks = new List<Task<bool>>();
            foreach (var order in asyncOrders)
                tasks.Add(asyncProcessor.ProcessOrderAsync(order, ct));
            foreach (var task in tasks)
                await task;

            swAsync.Stop();
            int asyncThreads = Log.DistinctThreads;
            int asyncPool = ThreadPool.ThreadCount;
            var asyncResult = await orderRepo.GetAllAsync(ct);

            // ---------- Summary ----------
            Console.WriteLine("\n===== Sync vs Async summary (orders #1-#10) =====");
            Console.WriteLine($"{"Version",-8}{"Time (s)",10}{"Distinct threads",20}{"ThreadPool.ThreadCount",26}{"Completed",11}{"Rejected",10}");
            Console.WriteLine($"{"Sync",-8}{swSync.Elapsed.TotalSeconds,10:0.0}{syncThreads,20}{syncPool,26}" +
                              $"{syncResult.Count(o => o.Status == OrderStatus.Completed),11}{syncResult.Count(o => o.Status == OrderStatus.Rejected),10}");
            Console.WriteLine($"{"Async",-8}{swAsync.Elapsed.TotalSeconds,10:0.0}{asyncThreads,20}{asyncPool,26}" +
                              $"{asyncResult.Count(o => o.Status == OrderStatus.Completed),11}{asyncResult.Count(o => o.Status == OrderStatus.Rejected),10}");
            Console.WriteLine("Expected for both: 8 Completed, 2 Rejected (#4, #9).");
        }
    }
}
