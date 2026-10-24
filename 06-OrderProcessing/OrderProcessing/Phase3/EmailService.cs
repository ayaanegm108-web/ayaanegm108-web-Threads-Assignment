using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class EmailService : IEmailService
    {
        private readonly JsonFileStore _store;
        private readonly IDictionary<int, int> _failAttempts;

        private readonly ConcurrentDictionary<int, int> _attemptCounts = new();

        public EmailService(JsonFileStore store, IDictionary<int, int> failAttempts)
        {
            _store = store;
            _failAttempts = failAttempts;
        }

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct)
        {
            int attempt = _attemptCounts.AddOrUpdate(order.Id, 1, (_, old) => old + 1);

            Log.Info($"EmailService -> Order #{order.Id}: rendering template (attempt {attempt})...");
            await Task.Delay(200, ct);

            Log.Info($"EmailService -> Order #{order.Id}: connecting to SMTP (attempt {attempt})...");
            await Task.Delay(300, ct);

            if (_failAttempts.TryGetValue(order.Id, out int failFirst) && attempt <= failFirst)
                throw new SmtpUnavailableException("SMTP server unavailable");

            Log.Info($"EmailService -> Order #{order.Id}: sending (attempt {attempt})...");
            await Task.Delay(500, ct);

            var message = BuildMessage(order, customer);
            message.Attempts = attempt;
            message.SentAt = DateTime.Now;

            await _store.UpdateAsync<EmailMessage>(DataFiles.Emails, list => list.Add(message), ct);

            Log.Success($"EmailService -> Order #{order.Id}: email sent to {customer.Email} (attempt {attempt})");
        }

        public async Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct)
        {
            int[] waitsBeforeRetry = { 500, 1000, 2000 };
            string lastError = "";

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                if (attempt > 1)
                {
                    int wait = waitsBeforeRetry[attempt - 2];
                    Log.Info($"EmailService -> Order #{order.Id}: waiting {wait} ms before retry {attempt - 1}...");
                    await Task.Delay(wait, ct);
                }

                try
                {
                    await SendConfirmationAsync(order, customer, ct);
                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    lastError = ex.Message;
                    Log.Error($"EmailService -> Order #{order.Id}: attempt {attempt} failed ({ex.Message})");
                }
            }

            var failed = BuildMessage(order, customer);
            failed.Attempts = 4;
            failed.LastError = lastError;   

            await _store.UpdateAsync<EmailMessage>(DataFiles.EmailRetry, list => list.Add(failed), ct);

            Log.Error($"Email for Order #{order.Id} queued for retry later");
        }

        private static EmailMessage BuildMessage(Order order, Customer customer) => new()
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} confirmation",
            Body = $"Hello {customer.Name}, your order #{order.Id} is confirmed. Total: {order.TotalAmount:0.00}"
        };
    }
}
