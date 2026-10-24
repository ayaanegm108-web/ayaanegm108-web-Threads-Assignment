using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase2
{
    public static class Phase2Runner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            var customers = new CustomerRepository(store);
            var products = new ProductRepository(store);
            var coupons = new CouponRepository(store);
            var orders = new OrderRepository(store);
            var incoming = new IncomingOrderRepository(store);

            var validator = new OrderValidator(customers, products, coupons, orders);
            var pricing = new PricingService(store, customers, coupons, new[] { 7 });
            var pricer = new OrderPricer(pricing);
            var processor = new OrderProcessorV2(products, orders, pricer, new InvoiceGenerator());
            var reportGenerator = new ReportGenerator(orders, products);

            await DataSeeder.ResetAsync(store, ct);
            await new RaceConditionDemo(store, products).RunAsync(ct);
            await DataSeeder.ResetAsync(store, ct);

            Console.WriteLine("\n===== Pricing comparison on order #1 (not saved) =====");
            var sample = (await incoming.GetAllAsync(ct)).First(o => o.Id == 1);

            var swSeq = Stopwatch.StartNew();
            await pricer.PriceOrderSequentialAsync(sample, ct);
            swSeq.Stop();

            var swCon = Stopwatch.StartNew();
            await pricer.PriceOrderAsync(sample, ct);
            swCon.Stop();

            Console.WriteLine($"Sequential pricing: {swSeq.Elapsed.TotalSeconds:0.0} s");
            Console.WriteLine($"Concurrent pricing: {swCon.Elapsed.TotalSeconds:0.0} s");
            Console.WriteLine($"Order #1 -> SubTotal {sample.SubTotal} / Discount {sample.Discount} / Shipping {sample.ShippingCost} / Total {sample.TotalAmount}");

            Console.WriteLine("\n===== Validating all 20 orders concurrently =====");
            var incomingOrders = await incoming.GetAllAsync(ct);   

            var validationTasks = new List<Task<bool>>();
            foreach (var order in incomingOrders)
                validationTasks.Add(validator.ValidateOrderAsync(order, ct));
            foreach (var task in validationTasks)
                await task;

            var toProcess = incomingOrders.Where(o => o.Status == OrderStatus.Validated).ToList();
            Console.WriteLine($"Validated orders to process: {toProcess.Count}");

            Console.WriteLine("\n===== Processing validated orders concurrently =====");
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task<bool>>();
            foreach (var order in toProcess)
                tasks.Add(processor.ProcessValidatedOrderAsync(order, ct));

            Console.WriteLine($"Status of task[0] right after creation: {tasks[0].Status}");

            await Task.Delay(1000, ct);
            Console.WriteLine("\nTask statuses after 1 second (still running):");
            for (int i = 0; i < tasks.Count; i++)
                Console.WriteLine($"  Order #{toProcess[i].Id}: {tasks[i].Status}");

            for (int i = 0; i < tasks.Count; i++)
            {
                var order = toProcess[i];
                try
                {
                    await tasks[i];
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Console.WriteLine($"\nOrder #{order.Id} task status: {tasks[i].Status} -> {ex.Message}");

                    foreach (var item in order.Items)
                        await products.ReleaseStockAsync(item.ProductId, item.Quantity, ct);

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;
                    await orders.SaveAsync(order, ct);
                }
            }
            sw.Stop();

            Console.WriteLine("\nFinal task statuses:");
            for (int i = 0; i < tasks.Count; i++)
                Console.WriteLine($"  Order #{toProcess[i].Id}: {tasks[i].Status}");
            Console.WriteLine($"Total processing time: {sw.Elapsed.TotalSeconds:0.0} seconds");

            var all = await orders.GetAllAsync(ct);
            Console.WriteLine($"\norders.json now contains {all.Count} records.");
            Console.WriteLine($"Completed: {all.Count(o => o.Status == OrderStatus.Completed)}, " +
                              $"Rejected: {all.Count(o => o.Status == OrderStatus.Rejected)}, " +
                              $"Failed: {all.Count(o => o.Status == OrderStatus.Failed)}");

            var reportTask = reportGenerator.GenerateAsync(ct);
            Console.WriteLine("\nGenerating report...");
            var report = await reportTask;

            Console.WriteLine("\n===== Report =====");
            Console.WriteLine($"Total orders: {report.TotalOrders}");
            Console.WriteLine($"Completed: {report.CompletedCount}, Rejected: {report.RejectedCount}, " +
                              $"Failed: {report.FailedCount}, PaymentFailed: {report.PaymentFailedCount}");
            Console.WriteLine($"Total revenue: {report.TotalRevenue}");
            Console.WriteLine($"Average order value: {report.AverageOrderValue}");
            Console.WriteLine($"Top selling product: {report.TopSellingProduct} ({report.TopSellingQuantity} units)");
            BlockingDemo.Run();
        }
    }
}
