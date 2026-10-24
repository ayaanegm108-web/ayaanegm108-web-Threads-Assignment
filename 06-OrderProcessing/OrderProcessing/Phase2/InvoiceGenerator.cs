using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class InvoiceGenerator
    {
        public async Task GenerateAsync(Order order, CancellationToken ct)
        {
            Log.Info($"InvoiceGenerator -> Order #{order.Id}: rendering invoice PDF...");

            long checksum = await Task.Run(() =>
            {
                long h = order.Id;
                for (int i = 0; i < 20_000_000; i++)
                {
                    h = (h * 31 + i) % 1_000_000_007;

                    if (i % 1_000_000 == 0)
                        ct.ThrowIfCancellationRequested();  
                }
                return h;
            }, ct);

            Directory.CreateDirectory(DataFiles.InvoicesFolder);
            string path = Path.Combine(DataFiles.InvoicesFolder, $"invoice_{order.Id}.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"INVOICE - Order #{order.Id}");
            sb.AppendLine($"Customer: #{order.CustomerId}");
            foreach (var item in order.Items)
                sb.AppendLine($"  Product #{item.ProductId} x {item.Quantity} @ {item.UnitPrice:0.00}");
            sb.AppendLine($"SubTotal: {order.SubTotal:0.00}");
            sb.AppendLine($"Discount: {order.Discount:0.00}");
            sb.AppendLine($"Shipping: {order.ShippingCost:0.00}");
            sb.AppendLine($"Total:    {order.TotalAmount:0.00}");
            sb.AppendLine($"Render checksum: {checksum}");

            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false), ct);

            Log.Success($"InvoiceGenerator -> Order #{order.Id}: invoice_{order.Id}.txt written");
        }
    }
}
