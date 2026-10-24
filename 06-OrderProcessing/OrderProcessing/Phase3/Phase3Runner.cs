using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase3
{
    public static class Phase3Runner
    {
        
        public static OrderProcessor BuildProcessor(JsonFileStore store, bool withFailures = true)
        {
            var customers = new CustomerRepository(store);
            var coupons = new CouponRepository(store);

            var timeoutIds = withFailures ? new[] { 5 } : Array.Empty<int>();
            var declineIds = withFailures ? new[] { 8 } : Array.Empty<int>();
            var failSaveIds = withFailures ? new[] { 13 } : Array.Empty<int>();
            var emailFailures = withFailures
                ? new Dictionary<int, int> { [3] = 2, [17] = int.MaxValue }
                : new Dictionary<int, int>();

            return new OrderProcessor(
                new CustomerService(customers),
                new InventoryService(store),
                new PricingService(store, customers, coupons, Array.Empty<int>()),
                new PaymentGateway(store, timeoutIds, declineIds),
                new OrderRepository(store, failSaveIds),
                new EmailService(store, emailFailures));
        }

        public static async Task RunAsync(CancellationToken ct, bool includeComparison = true)
        {
            if (includeComparison)
            {
                await SyncVsAsyncComparison.RunAsync(ct);
                await FireAndForgetDemo.RunAsync(ct);
            }

            var store = new JsonFileStore();
            var processor = BuildProcessor(store);
            var incoming = new IncomingOrderRepository(store);
            var orders = new Phase1.OrderRepository(store);

            await DataSeeder.ResetAsync(store, ct);
            var all = await incoming.GetAllAsync(ct);

            Console.WriteLine("\n===== Phase 3: full checkout for all orders (concurrent) =====");
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task<bool>>();
            var started = new List<Order>();
            foreach (var order in all)
            {
                if (ct.IsCancellationRequested) break;   
                started.Add(order);
                tasks.Add(processor.ProcessOrderAsync(order, ct));
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                try
                {
                    await tasks[i];
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Order #{started[i].Id}: task faulted unexpectedly ({ex.GetType().Name}: {ex.Message})");
                }
            }
            sw.Stop();

            var final = (await orders.GetAllAsync(CancellationToken.None)).OrderBy(o => o.Id).ToList();

            Console.WriteLine("\n===== Final table =====");
            Console.WriteLine($"{"Order",-7}{"Status",-15}{"Total",10}  Reason");
            foreach (var o in final)
                Console.WriteLine($"{"#" + o.Id,-7}{o.Status,-15}{o.TotalAmount,10:0.00}  {o.FailureReason ?? "-"}");

            Console.WriteLine($"\norders.json now contains {final.Count} records.");
            foreach (var g in final.GroupBy(o => o.Status).OrderBy(g => g.Key))
                Console.WriteLine($"  {g.Key}: {g.Count()}");

            var payments = await store.ReadAllAsync<PaymentTransaction>(DataFiles.Payments, CancellationToken.None);
            var emails = await store.ReadAllAsync<EmailMessage>(DataFiles.Emails, CancellationToken.None);
            var retry = await store.ReadAllAsync<EmailMessage>(DataFiles.EmailRetry, CancellationToken.None);
            var products = await store.ReadAllAsync<Product>(DataFiles.Products, CancellationToken.None);

            Console.WriteLine("\n===== Files check =====");
            Console.WriteLine($"payments.json    : {payments.Count} records ({string.Join(", ", payments.GroupBy(p => p.Status).Select(g => $"{g.Key} {g.Count()}"))})");
            Console.WriteLine($"emails.json      : {emails.Count} records");
            Console.WriteLine($"email_retry.json : {retry.Count} records");
            Console.WriteLine($"Mechanical Keyboard stock: {products.First(p => p.Id == 4).StockQuantity}");
            Console.WriteLine($"Total time: {sw.Elapsed.TotalSeconds:0.0} seconds");

            if (ct.IsCancellationRequested)
            {
                int cancelled = final.Count(o => o.Status == OrderStatus.Pending && o.FailureReason == "Cancelled");
                int finished = final.Count(o => o.Status != OrderStatus.Pending);
                Console.WriteLine($"Run cancelled — {finished} orders finished, {cancelled} cancelled.");
            }
        }
    }
}

