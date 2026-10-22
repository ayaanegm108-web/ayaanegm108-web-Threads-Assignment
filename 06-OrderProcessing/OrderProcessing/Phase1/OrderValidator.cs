using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class OrderValidator
    {
        private readonly CustomerRepository _customers;
        private readonly ProductRepository _products;
        private readonly CouponRepository _coupons;
        private readonly OrderRepository _orders;

        public OrderValidator(CustomerRepository customers, ProductRepository products,
                              CouponRepository coupons, OrderRepository orders)
        {
            _customers = customers;
            _products = products;
            _coupons = coupons;
            _orders = orders;
        }

       
        public async Task<bool> ValidateOrderAsync(Order order, CancellationToken ct)
        {
            var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
            if (customer is null)
                return await RejectAsync(order, $"Customer #{order.CustomerId} not found", ct);

           
            if (!customer.IsActive)
                return await RejectAsync(order, "Customer is inactive", ct);

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
            await _orders.SaveAsync(order, ct);
            Log.Success($"OrderValidator -> Order #{order.Id} validated");
            return true;
        }

        private async Task<bool> RejectAsync(Order order, string reason, CancellationToken ct)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;
            await _orders.SaveAsync(order, ct);
            Log.Error($"OrderValidator -> Order #{order.Id} rejected: {reason}");
            return false;
        }
    }
}
