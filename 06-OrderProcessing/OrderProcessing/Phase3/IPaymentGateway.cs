using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public interface IPaymentGateway
    {
        Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct);
        Task RefundAsync(string transactionId, CancellationToken ct);
    }
}
