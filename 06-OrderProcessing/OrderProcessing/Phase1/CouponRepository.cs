using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class CouponRepository
    {
        private readonly JsonFileStore _store;

        public CouponRepository(JsonFileStore store) => _store = store;

        public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
        {
            Log.Info("CouponRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"CouponRepository -> Executing query for Coupon '{code}'...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var coupons = await _store.ReadAllAsync<Coupon>(DataFiles.Coupons, ct);
            var coupon = coupons.FirstOrDefault(c =>
                string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

            if (coupon is null)
                Log.Info($"CouponRepository -> Coupon '{code}' not found");
            else
                Log.Success($"CouponRepository -> Coupon '{coupon.Code}' found ({coupon.DiscountPercent}%)");

            return coupon;
        }
    }
}
