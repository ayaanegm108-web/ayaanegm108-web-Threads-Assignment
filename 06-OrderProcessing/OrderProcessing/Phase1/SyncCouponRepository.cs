using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class SyncCouponRepository
    {
        private readonly JsonFileStore _store;

        public SyncCouponRepository(JsonFileStore store) => _store = store;

        public  Coupon? GetByCode(string code)
        {
            Log.Info("CouponRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"CouponRepository -> Executing query for Coupon '{code}'...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var coupons = _store.ReadAll<Coupon>(DataFiles.Coupons);
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
