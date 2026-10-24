using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3
{
    public class SyncOrderProcessor
    {
        private readonly SyncCustomerRepository _customers;
        private readonly SyncInventoryService _inventory;
        private readonly SyncPricingService _pricing;
        private readonly SyncPaymentGateway _payments;
        private readonly OrderProcessing.Phase1.SyncOrderRepository _orders;
        private readonly SyncEmailService _emails;

        public SyncOrderProcessor(SyncCustomerRepository customers,
                                  SyncInventoryService inventory,
                                  SyncPricingService pricing,
                                  SyncPaymentGateway payments,
                                  SyncOrderRepository orders,
                                  SyncEmailService emails)
        {
            _customers = customers;
            _inventory = inventory;
            _pricing = pricing;
            _payments = payments;
            _orders = orders;
            _emails = emails;
        }

        public bool ProcessOrder(Order order)
        {
            var customer = _customers.GetById(order.CustomerId);
            if (customer is null)
                return Finish(order, OrderStatus.Rejected, $"Customer #{order.CustomerId} not found");
            if (!customer.IsActive)
                return Finish(order, OrderStatus.Rejected, "Customer is inactive");

            if (!_inventory.ReserveStock(order))
                return Finish(order, OrderStatus.Rejected, order.FailureReason ?? "Insufficient stock");

            _pricing.CalculateTotal(order);

            var payment = _payments.ProcessPayment(order.Id, order.TotalAmount);
            if (!payment.Success)
            {
                _inventory.ReleaseStock(order);
                return Finish(order, OrderStatus.PaymentFailed, payment.Message ?? "Card declined by issuer");
            }

            order.TransactionId = payment.TransactionId;
            Finish(order, OrderStatus.Completed, null);

            _emails.SendConfirmation(order, customer);
            Log.Success($"SyncOrderProcessor -> Order #{order.Id} completed");
            return true;
        }

        private bool Finish(Order order, OrderStatus status, string? reason)
        {
            order.Status = status;
            order.FailureReason = reason;
           _orders.Save(order);
            return status == OrderStatus.Completed;
        }
    }
}

