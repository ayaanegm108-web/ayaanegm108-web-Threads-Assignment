using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class OrderPricer
    {
        private readonly PricingService _pricing;

        public OrderPricer(PricingService pricing) => _pricing = pricing;

        private static decimal Round(decimal x)
            => Math.Round(x, 2, MidpointRounding.AwayFromZero);

       
        public async Task PriceOrderAsync(Order order, CancellationToken ct)
        {
           
            await _pricing.CalculateSubTotalAsync(order, ct);

            
            var discountTask = _pricing.GetDiscountAsync(order, ct);
            var shippingTask = _pricing.CalculateShippingCostAsync(order, ct);

           
            await discountTask;
            await shippingTask;

            order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
            Log.Success($"OrderPricer -> Order #{order.Id}: Total = {order.TotalAmount}");
        }

        
        public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            await _pricing.CalculateSubTotalAsync(order, ct);

            await _pricing.GetDiscountAsync(order, ct);            
            await _pricing.CalculateShippingCostAsync(order, ct); 

            order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
            Log.Success($"OrderPricer -> Order #{order.Id}: Total = {order.TotalAmount} (sequential)");
        }
    }
}
