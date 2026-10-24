using OrderProcessing.Phase1;
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
    }
}
