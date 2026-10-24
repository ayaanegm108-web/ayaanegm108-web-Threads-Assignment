using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _timeoutOrderIds;
        private readonly HashSet<int> _declineOrderIds;

        public PaymentGateway(JsonFileStore store,
                              IEnumerable<int> timeoutOrderIds,
                              IEnumerable<int> declineOrderIds)
        {
            _store = store;
            _timeoutOrderIds = new HashSet<int>(timeoutOrderIds);
            _declineOrderIds = new HashSet<int>(declineOrderIds);
        }

        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Order #{orderId}: authorizing...");
            await Task.Delay(800, ct);

            Log.Info($"PaymentGateway -> Order #{orderId}: fraud check...");
            await Task.Delay(500, ct);

            if (_declineOrderIds.Contains(orderId))
            {
                await AppendAsync(new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = PaymentStatus.Declined,
                    TransactionId = null,
                    CreatedAt = DateTime.Now
                }, ct);

                Log.Error($"PaymentGateway -> Order #{orderId}: declined (Card declined by issuer)");
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = null,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }

            Log.Info($"PaymentGateway -> Order #{orderId}: capturing...");
            await Task.Delay(_timeoutOrderIds.Contains(orderId) ? 5000 : 700, ct);

            string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

            await AppendAsync(new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.Now
            }, ct);

            Log.Success($"PaymentGateway -> Order #{orderId}: captured ({transactionId})");
            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured"
            };
        }

        public async Task RefundAsync(string transactionId, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Refund {transactionId}: DNS/handshake...");
            await Task.Delay(150, ct);

            Log.Info($"PaymentGateway -> Refund {transactionId}: sending request...");
            await Task.Delay(300, ct);

            Log.Info($"PaymentGateway -> Refund {transactionId}: waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            bool found = false;
            await _store.UpdateAsync<PaymentTransaction>(DataFiles.Payments, payments =>
            {
                var record = payments.FirstOrDefault(p => p.TransactionId == transactionId);
                if (record is null) return;

                record.Status = PaymentStatus.Refunded;
                found = true;
            }, ct);

            if (found)
                Log.Success($"PaymentGateway -> Refund {transactionId}: refunded");
            else
                Log.Error($"PaymentGateway -> Refund {transactionId}: transaction not found");
        }

        private Task AppendAsync(PaymentTransaction transaction, CancellationToken ct)
            => _store.UpdateAsync<PaymentTransaction>(DataFiles.Payments, list => list.Add(transaction), ct);
    }
}
