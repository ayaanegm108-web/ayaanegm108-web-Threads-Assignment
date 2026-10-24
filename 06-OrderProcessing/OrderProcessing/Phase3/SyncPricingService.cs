using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public class SyncPricingService
    {
        private readonly JsonFileStore _store;
        private readonly SyncCustomerRepository _customers;
        private readonly SyncCouponRepository _coupons;

        public SyncPricingService(JsonFileStore store, SyncCustomerRepository customers, SyncCouponRepository coupons)
        {
            _store = store;
            _customers = customers;
            _coupons = coupons;
        }

        private static decimal Round(decimal x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);

        public decimal CalculateTotal(Order order)
        {
            Log.Info($"SyncPricingService -> Order #{order.Id}: querying product prices...");
            Thread.Sleep(200);
            Thread.Sleep(Random.Shared.Next(300, 601));
            var products = _store.ReadAll<Product>(DataFiles.Products);

            decimal subTotal = 0;
            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
            }
            order.SubTotal = Round(subTotal);

            Log.Info($"SyncPricingService -> Order #{order.Id}: Discount API...");
            Thread.Sleep(150);
            Thread.Sleep(300);
            Thread.Sleep(Random.Shared.Next(500, 1501));

            var customer = _customers.GetById(order.CustomerId);
            int percent = customer?.LoyaltyTier switch
            {
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Gold => 10,
                _ => 0
            };

            if (order.CouponCode is not null)
            {
                var coupon = _coupons.GetByCode(order.CouponCode)
                    ?? throw new InvalidCouponException($"Coupon '{order.CouponCode}' does not exist");
                if (coupon.ExpiryDate < DateTime.Today)
                    throw new InvalidCouponException($"Coupon '{order.CouponCode}' has expired");
                percent += coupon.DiscountPercent;
            }
            percent = Math.Min(percent, 30);
            order.Discount = Round(order.SubTotal * percent / 100m);

            Log.Info($"SyncPricingService -> Order #{order.Id}: Shipping API...");
            Thread.Sleep(150);
            Thread.Sleep(300);
            Thread.Sleep(Random.Shared.Next(500, 1501));

            int totalQty = 0;
            decimal totalWeight = 0;
            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                totalQty += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            decimal shipping = order.SubTotal >= 1000 ? 0m : 5.00m + 1.50m * totalQty + 2.00m * totalWeight;
            order.ShippingCost = Round(shipping);

            order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
            Log.Success($"SyncPricingService -> Order #{order.Id}: Total = {order.TotalAmount}");
            return order.TotalAmount;
        }
    }
}
