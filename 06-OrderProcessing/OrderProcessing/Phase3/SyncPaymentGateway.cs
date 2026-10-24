using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class SyncPaymentGateway
    {
        private readonly JsonFileStore _store;

        public SyncPaymentGateway(JsonFileStore store) => _store = store;

        public PaymentResult ProcessPayment(int orderId, decimal amount)
        {
            Log.Info($"SyncPaymentGateway -> Order #{orderId}: authorizing...");
            Thread.Sleep(800);
            Log.Info($"SyncPaymentGateway -> Order #{orderId}: fraud check...");
            Thread.Sleep(500);
            Log.Info($"SyncPaymentGateway -> Order #{orderId}: capturing...");
            Thread.Sleep(700);

            string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

            _store.Update<PaymentTransaction>(DataFiles.Payments, list => list.Add(new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.Now
            }));

            Log.Success($"SyncPaymentGateway -> Order #{orderId}: captured ({transactionId})");
            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured"
            };
        }
    }
}
