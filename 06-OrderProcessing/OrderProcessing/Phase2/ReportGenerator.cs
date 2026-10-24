using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderProcessing.Phase2
{
    public class ReportGenerator
    {
        private readonly OrderRepository _orders;
        private readonly ProductRepository _products;

        public ReportGenerator(OrderRepository orders, ProductRepository products)
        {
            _orders = orders;
            _products = products;
        }

        public async Task<Report> GenerateAsync(CancellationToken ct)
        {
            Log.Info("ReportGenerator -> loading orders...");
            var orders = await _orders.GetAllAsync(ct);

            var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

            var report = new Report
            {
                TotalOrders = orders.Count,
                CompletedCount = completed.Count,
                RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected),
                FailedCount = orders.Count(o => o.Status == OrderStatus.Failed),
                PaymentFailedCount = orders.Count(o => o.Status == OrderStatus.PaymentFailed),
                TotalRevenue = completed.Sum(o => o.TotalAmount),
                GeneratedAt = DateTime.Now
            };

            report.AverageOrderValue = completed.Count == 0
                ? 0m
                : Math.Round(report.TotalRevenue / completed.Count, 2, MidpointRounding.AwayFromZero);

            var top = completed
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .ThenBy(x => x.ProductId)
                .FirstOrDefault();

            if (top is not null)
            {
                Log.Info("ReportGenerator -> loading top-selling product...");
                var product = await _products.GetByIdAsync(top.ProductId, ct);
                report.TopSellingProduct = product?.Name;
                report.TopSellingQuantity = top.Quantity;
            }


            Directory.CreateDirectory(DataFiles.ReportsFolder);
            string path = Path.Combine(DataFiles.ReportsFolder,
                $"report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json");

            string json = JsonSerializer.Serialize(report, JsonFileStore.Options);
            await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);

            Log.Success($"ReportGenerator -> report saved ({Path.GetFileName(path)})");
            return report;
        }
    }
}
