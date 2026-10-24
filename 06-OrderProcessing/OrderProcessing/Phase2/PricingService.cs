using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class PricingService
    {
        private readonly JsonFileStore _store;
        private readonly CustomerRepository _customers;
        private readonly CouponRepository _coupons;
        private readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(JsonFileStore store,
                              CustomerRepository customers,
                              CouponRepository coupons,
                              IEnumerable<int> failShippingForOrderIds)
        {
            _store = store;
            _customers = customers;
            _coupons = coupons;
            _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds);
        }

        private static decimal Round(decimal x)
            => Math.Round(x, 2, MidpointRounding.AwayFromZero);

        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            Log.Info($"PricingService -> Order #{order.Id}: opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"PricingService -> Order #{order.Id}: querying product prices...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);

            decimal subTotal = 0;
            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
            }

            order.SubTotal = Round(subTotal);
            Log.Success($"PricingService -> Order #{order.Id}: SubTotal = {order.SubTotal}");
            return order.SubTotal;
        }


        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            Log.Info($"PricingService -> Order #{order.Id}: Discount API DNS/handshake...");
            await Task.Delay(150, ct);

            Log.Info($"PricingService -> Order #{order.Id}: Discount API sending request...");
            await Task.Delay(300, ct);

            Log.Info($"PricingService -> Order #{order.Id}: Discount API waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            var customer = await _customers.GetByIdAsync(order.CustomerId, ct);

            int percent = customer?.LoyaltyTier switch
            {
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Gold => 10,
                _ => 0
            };

            if (order.CouponCode is not null)
            {
                var coupon = await _coupons.GetByCodeAsync(order.CouponCode, ct);
                if (coupon is not null)
                    percent += coupon.DiscountPercent;
            }

            percent = Math.Min(percent, 30);

            order.Discount = Round(order.SubTotal * percent / 100m);
            Log.Success($"PricingService -> Order #{order.Id}: Discount = {order.Discount} ({percent}%)");
            return order.Discount;
        }


        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            Log.Info($"PricingService -> Order #{order.Id}: Shipping API DNS/handshake...");
            await Task.Delay(150, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                Log.Error($"PricingService -> Order #{order.Id}: Shipping provider unavailable");
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            Log.Info($"PricingService -> Order #{order.Id}: Shipping API sending request...");
            await Task.Delay(300, ct);

            Log.Info($"PricingService -> Order #{order.Id}: Shipping API waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);

            int totalQuantity = 0;
            decimal totalWeight = 0;
            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            decimal shipping = order.SubTotal >= 1000
                ? 0m
                : 5.00m + 1.50m * totalQuantity + 2.00m * totalWeight;

            order.ShippingCost = Round(shipping);
            Log.Success($"PricingService -> Order #{order.Id}: Shipping = {order.ShippingCost}");
            return order.ShippingCost;
        }
    }
}
