using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class OrderProcessor
    {
        private const int TotalSteps = 6;
        private const int PaymentTimeoutMs = 3000;

        private readonly CustomerService _customers;
        private readonly InventoryService _inventory;
        private readonly PricingService _pricing;
        private readonly IPaymentGateway _payments;
        private readonly OrderRepository _orders;
        private readonly IEmailService _emails;

        public OrderProcessor(CustomerService customers,
                              InventoryService inventory,
                              PricingService pricing,
                              IPaymentGateway payments,
                              OrderRepository orders,
                              IEmailService emails)
        {
            _customers = customers;
            _inventory = inventory;
            _pricing = pricing;
            _payments = payments;
            _orders = orders;
            _emails = emails;
        }

        public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct)
        {
            bool stockReserved = false;
            string? transactionId = null;
            var sw = new Stopwatch();

            try
            {
                sw.Restart();
                Customer customer;
                try
                {
                    customer = await _customers.GetCustomerAsync(order.CustomerId, ct);
                }
                catch (CustomerNotFoundException ex)
                {
                    StepFailed(order, 1, "Customer", ex.Message, sw);
                    await FinishAsync(order, OrderStatus.Rejected, ex.Message, ct);
                    return false;
                }

                if (!customer.IsActive)
                {
                    StepFailed(order, 1, "Customer", "Customer is inactive", sw);
                    await FinishAsync(order, OrderStatus.Rejected, "Customer is inactive", ct);
                    return false;
                }
                StepOk(order, 1, "Customer", $"({customer.Name})", sw);

                sw.Restart();
                if (!await _inventory.ReserveStockAsync(order, ct))
                {
                    string reason = order.FailureReason ?? "Insufficient stock";
                    StepFailed(order, 2, "Stock", reason, sw);
                    await FinishAsync(order, OrderStatus.Rejected, reason, ct);
                    return false;
                }
                stockReserved = true;
                StepOk(order, 2, "Stock", "", sw);

                sw.Restart();
                try
                {
                    await _pricing.CalculateTotalAsync(order, ct);
                }
                catch (InvalidCouponException ex)
                {
                    await ReleaseAsync(order);
                    stockReserved = false;
                    StepFailed(order, 3, "Pricing", ex.Message, sw);
                    await FinishAsync(order, OrderStatus.Rejected, ex.Message, ct);
                    return false;
                }
                StepOk(order, 3, "Pricing", $"(Total {order.TotalAmount})", sw);

                sw.Restart();
                PaymentResult payment;
                using (var paymentCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    paymentCts.CancelAfter(PaymentTimeoutMs);
                    try
                    {
                        payment = await _payments.ProcessPaymentAsync(order.Id, order.TotalAmount, paymentCts.Token);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        long waitedMs = sw.ElapsedMilliseconds;   
                        await ReleaseAsync(order);
                        stockReserved = false;
                        Log.Error($"Order #{order.Id} -> Step 4/{TotalSteps} Payment ... TIMEOUT after {waitedMs} ms, released");
                        await FinishAsync(order, OrderStatus.PaymentFailed, "Payment gateway timeout (3s)", ct);
                        return false;
                    }
                }

                if (!payment.Success)
                {
                    await ReleaseAsync(order);
                    stockReserved = false;
                    StepFailed(order, 4, "Payment", payment.Message ?? "Declined", sw);
                    await FinishAsync(order, OrderStatus.PaymentFailed, payment.Message ?? "Card declined by issuer", ct);
                    return false;
                }
                transactionId = payment.TransactionId;
                StepOk(order, 4, "Payment", $"({transactionId})", sw);

                sw.Restart();
                order.TransactionId = transactionId;
                order.Status = OrderStatus.Completed;
                order.FailureReason = null;
                try
                {
                    await _orders.SaveAsync(order, ct);
                }
                catch (DatabaseWriteException ex)
                {
                    StepFailed(order, 5, "Save", ex.Message, sw);

                    await _payments.RefundAsync(transactionId!, CancellationToken.None);
                    await ReleaseAsync(order);

                    string reason = $"Database save failed after payment; payment {transactionId} refunded";
                    transactionId = null;
                    stockReserved = false;
                    await FinishAsync(order, OrderStatus.Failed, reason, CancellationToken.None);
                    return false;
                }
                StepOk(order, 5, "Save", "", sw);

                sw.Restart();
                try
                {
                    await _emails.SendConfirmationWithRetryAsync(order, customer, ct);
                    StepOk(order, 6, "Email", "", sw);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    StepFailed(order, 6, "Email", ex.Message, sw);
                }

                return order.Status == OrderStatus.Completed;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await CancelAsync(order, stockReserved, transactionId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Order #{order.Id} -> unexpected error: {ex.Message}");
                if (transactionId is not null)
                    await _payments.RefundAsync(transactionId, CancellationToken.None);
                if (stockReserved)
                    await ReleaseAsync(order);

                await FinishAsync(order, OrderStatus.Failed, ex.Message, CancellationToken.None);
                return false;
            }
        }

        private async Task FinishAsync(Order order, OrderStatus status, string reason, CancellationToken ct)
        {
            order.Status = status;
            order.FailureReason = reason;
            await _orders.SaveAsync(order, ct);
        }

        private Task ReleaseAsync(Order order)
            => _inventory.ReleaseStockAsync(order, CancellationToken.None);

        private async Task CancelAsync(Order order, bool stockReserved, string? transactionId)
        {
            Log.Error($"Order #{order.Id} -> cancelled, cleaning up...");

            if (transactionId is not null)
                await _payments.RefundAsync(transactionId, CancellationToken.None);

            if (stockReserved)
                await ReleaseAsync(order);

            order.TransactionId = null;
            await FinishAsync(order, OrderStatus.Pending, "Cancelled", CancellationToken.None);
        }

        private static void StepOk(Order order, int step, string name, string detail, Stopwatch sw)
            => Log.Success($"Order #{order.Id} -> Step {step}/{TotalSteps} {name} ... OK {detail} in {sw.ElapsedMilliseconds} ms");

        private static void StepFailed(Order order, int step, string name, string reason, Stopwatch sw)
            => Log.Error($"Order #{order.Id} -> Step {step}/{TotalSteps} {name} ... FAILED ({reason}) in {sw.ElapsedMilliseconds} ms");
    }
}

