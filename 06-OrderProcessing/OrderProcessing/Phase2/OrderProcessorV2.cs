using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class OrderProcessorV2
    {
        private readonly ProductRepository _products;
        private readonly OrderRepository _orders;
        private readonly OrderPricer _pricer;
        private readonly InvoiceGenerator _invoiceGenerator;

        public OrderProcessorV2(ProductRepository products,
                                OrderRepository orders,
                                OrderPricer pricer,
                                InvoiceGenerator invoiceGenerator)
        {
            _products = products;
            _orders = orders;
            _pricer = pricer;
            _invoiceGenerator = invoiceGenerator;
        }

        public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct)
        {
            var reserved = new List<OrderItem>();

            foreach (var item in order.Items)
            {
                bool ok = await _products.ReserveStockAsync(item.ProductId, item.Quantity, ct);

                if (!ok)
                {
                    foreach (var done in reserved)
                        await _products.ReleaseStockAsync(done.ProductId, done.Quantity, ct);

                    var product = await _products.GetByIdAsync(item.ProductId, ct);
                    string name = product?.Name ?? $"#{item.ProductId}";

                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{name}'";
                    await _orders.SaveAsync(order, ct);

                    Log.Error($"OrderProcessorV2 -> Order #{order.Id} rejected: {order.FailureReason}");
                    return false;
                }

                reserved.Add(item);
            }

            await _pricer.PriceOrderAsync(order, ct);

            await _invoiceGenerator.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;
            order.FailureReason = null;
            await _orders.SaveAsync(order, ct);

            Log.Success($"OrderProcessorV2 -> Order #{order.Id} completed (Total {order.TotalAmount})");
            return true;
        }
    }
}
