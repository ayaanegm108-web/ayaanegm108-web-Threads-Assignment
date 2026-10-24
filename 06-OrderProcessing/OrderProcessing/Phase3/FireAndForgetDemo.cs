using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class BrokenOrderFlow
    {
        private readonly OrderRepository _orderRepository;
        private readonly IEmailService _emailService;

        public BrokenOrderFlow(OrderRepository orderRepository, IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
        }

#pragma warning disable CS4014
        public async Task ProcessOrderAsync(Order order, Customer customer, CancellationToken ct)
        {
            await _orderRepository.SaveAsync(order, ct);
            _emailService.SendConfirmationAsync(order, customer, ct); 
            Console.WriteLine("Order processing finished");
        }
#pragma warning restore CS4014
    }

    public class FixedOrderFlow
    {
        private readonly OrderRepository _orderRepository;
        private readonly IEmailService _emailService;

        public FixedOrderFlow(OrderRepository orderRepository, IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
        }

        public async Task ProcessOrderAsync(Order order, Customer customer, CancellationToken ct)
        {
            await _orderRepository.SaveAsync(order, ct);

            try
            {
                await _emailService.SendConfirmationAsync(order, customer, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Order #{order.Id}: email failed ({ex.Message}) - order is still saved");
            }

            Console.WriteLine("Order processing finished");
        }
    }

    public static class FireAndForgetDemo
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            var orders = new OrderRepository(store);
            var customer = MakeCustomer();

            EventHandler<UnobservedTaskExceptionEventArgs> unobserved = (_, e) =>
            {
                Log.Error($"UnobservedTaskException raised: {e.Exception.InnerException?.Message}");
                e.SetObserved();
            };
            TaskScheduler.UnobservedTaskException += unobserved;

            try
            {
                Console.WriteLine("\n===== 9.7 A) BROKEN: method returns while the email is still being sent =====");
                await DataSeeder.ResetAsync(store, ct);
                var broken = new BrokenOrderFlow(orders, new EmailService(store, new Dictionary<int, int>()));

                await broken.ProcessOrderAsync(MakeOrder(9001), customer, ct);
                Console.WriteLine($"emails.json right after the method returned: {await CountEmailsAsync(store, ct)} record(s)");
                await Task.Delay(1500, ct);
                Console.WriteLine($"emails.json 1.5 s later: {await CountEmailsAsync(store, ct)} record(s)");

                Console.WriteLine("\n===== 9.7 B) BROKEN: the email throws an exception =====");
                await DataSeeder.ResetAsync(store, ct);
                var brokenFailing = new BrokenOrderFlow(orders,
                    new EmailService(store, new Dictionary<int, int> { [9002] = int.MaxValue }));

                await brokenFailing.ProcessOrderAsync(MakeOrder(9002), customer, ct);
                await Task.Delay(1500, ct);  
                Console.WriteLine("The caller got NO exception and believes everything worked.");
                Console.WriteLine($"emails.json: {await CountEmailsAsync(store, ct)} record(s) (the email was lost)");

                Console.WriteLine("Forcing garbage collection so the never-observed task is finalized...");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(300, ct);

                Console.WriteLine("\n===== 9.7 C) FIXED: await the email =====");
                await DataSeeder.ResetAsync(store, ct);
                var fixedFlow = new FixedOrderFlow(orders, new EmailService(store, new Dictionary<int, int>()));

                await fixedFlow.ProcessOrderAsync(MakeOrder(9003), customer, ct);
                Console.WriteLine($"emails.json right after the method returned: {await CountEmailsAsync(store, ct)} record(s)");

                Console.WriteLine("\n===== 9.7 D) FIXED: the email fails, the failure is handled =====");
                await DataSeeder.ResetAsync(store, ct);
                var fixedFailing = new FixedOrderFlow(orders,
                    new EmailService(store, new Dictionary<int, int> { [9004] = int.MaxValue }));

                await fixedFailing.ProcessOrderAsync(MakeOrder(9004), customer, ct);
                var saved = await orders.GetAllAsync(ct);
                Console.WriteLine($"orders.json: {saved.Count} record(s), order #9004 status = {saved.First().Status}");
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= unobserved;
            }
        }

        public static async Task RunExitDemoAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            await DataSeeder.ResetAsync(store, ct);

            var broken = new BrokenOrderFlow(new OrderRepository(store),
                new EmailService(store, new Dictionary<int, int>()));

            await broken.ProcessOrderAsync(MakeOrder(9005), MakeCustomer(), ct);

            Console.WriteLine("Main is returning NOW - the process exits while the email is still being sent.");
            Console.WriteLine($"Check emails.json after the program ends: {DataFiles.DataFolder}");
        }

        private static Order MakeOrder(int id) => new()
        {
            Id = id,
            CustomerId = 1,
            Status = OrderStatus.Completed,
            TotalAmount = 100m,
            CreatedAt = DateTime.Now,
            Items = { new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 100m } }
        };

        private static Customer MakeCustomer() => new()
        {
            Id = 1,
            Name = "Ahmed Ali",
            Email = "ahmed.ali@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Gold
        };

        private static async Task<int> CountEmailsAsync(JsonFileStore store, CancellationToken ct)
            => (await store.ReadAllAsync<EmailMessage>(DataFiles.Emails, ct)).Count;
    }
}
