using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase1
{
    public static class Phase1Runner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();

            // الـ async repositories والـ validator
            var customers = new CustomerRepository(store);
            var products = new ProductRepository(store);
            var coupons = new CouponRepository(store);
            var orders = new OrderRepository(store);
            var incoming = new IncomingOrderRepository(store);
            var asyncValidator = new OrderValidator(customers, products, coupons, orders);

            // الـ sync repositories والـ validator
            var syncValidator = new SyncOrderValidator(
                new SyncCustomerRepository(store),
                new SyncProductRepository(store),
                new SyncCouponRepository(store));

            // 1) reset + تحميل الـ 20 أوردر
            await DataSeeder.ResetAsync(store, ct);
            var incomingOrders = await incoming.GetAllAsync(ct);

            // 2) Sync run (من غير حفظ)
            Console.WriteLine("\n--- Sync run ---");
            var swSync = Stopwatch.StartNew();
            foreach (var order in incomingOrders)
                syncValidator.ValidateOrder(order);
            swSync.Stop();

            // 3) reset تاني
            await DataSeeder.ResetAsync(store, ct);

            // 4) Async run (الحفظ بيحصل جوه ValidateOrderAsync)
            Console.WriteLine("\n--- Async run ---");
            var swAsync = Stopwatch.StartNew();
            foreach (var order in incomingOrders)
                await asyncValidator.ValidateOrderAsync(order, ct);
            swAsync.Stop();

            // 5) الـ Summary من orders.json مش من الذاكرة
            var all = await orders.GetAllAsync(ct);
            var validated = await orders.GetByStatusAsync(OrderStatus.Validated, ct);
            var rejected = await orders.GetByStatusAsync(OrderStatus.Rejected, ct);

            Console.WriteLine();
            Console.WriteLine("===== Validation Summary =====");
            Console.WriteLine($"Total Orders: {incomingOrders.Count}");
            Console.WriteLine($"Validated: {validated.Count}");
            Console.WriteLine($"Rejected: {rejected.Count}");
            foreach (var o in rejected.OrderBy(o => o.Id))
                Console.WriteLine($" - Order #{o.Id}: {o.FailureReason}");
            Console.WriteLine($"Sync Time: {swSync.Elapsed.TotalSeconds:0.0} seconds");
            Console.WriteLine($"Async Time: {swAsync.Elapsed.TotalSeconds:0.0} seconds");
            Console.WriteLine($"orders.json now contains {all.Count} records.");
        }

    }
}
