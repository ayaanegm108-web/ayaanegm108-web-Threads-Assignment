using System;
using System.Collections.Generic;
using System.Text;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Logging;
namespace OrderProcessing.Phase3
{
    public class SyncEmailService
    {
        private readonly JsonFileStore _store;

        public SyncEmailService(JsonFileStore store) => _store = store;

        public void SendConfirmation(Order order, Customer customer)
        {
            Log.Info($"SyncEmailService -> Order #{order.Id}: rendering template...");
            Thread.Sleep(200);
            Log.Info($"SyncEmailService -> Order #{order.Id}: connecting to SMTP...");
            Thread.Sleep(300);
            Log.Info($"SyncEmailService -> Order #{order.Id}: sending...");
            Thread.Sleep(500);

            var message = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} confirmation",
                Body = $"Hello {customer.Name}, your order #{order.Id} is confirmed. Total: {order.TotalAmount:0.00}",
                Attempts = 1,
                SentAt = DateTime.Now
            };

            _store.Update<EmailMessage>(DataFiles.Emails, list => list.Add(message));
            Log.Success($"SyncEmailService -> Order #{order.Id}: email sent to {customer.Email}");
        }
    }
}
