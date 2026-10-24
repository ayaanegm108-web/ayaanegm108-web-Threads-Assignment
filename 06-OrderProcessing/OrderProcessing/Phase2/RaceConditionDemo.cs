using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class RaceConditionDemo
    {
        private readonly JsonFileStore _store;
        private readonly ProductRepository _products;

        public RaceConditionDemo(JsonFileStore store, ProductRepository products)
        {
            _store = store;
            _products = products;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== Race condition demo: UNSAFE reservation =====");
            await SetKeyboardStockAsync(1, ct);

            var task10 = _products.ReserveStockUnsafeAsync(4, 1, ct);   
            var task11 = _products.ReserveStockUnsafeAsync(4, 1, ct);   
            bool result10 = await task10;
            bool result11 = await task11;

            Console.WriteLine($"Order #10 got the keyboard: {result10}");
            Console.WriteLine($"Order #11 got the keyboard: {result11}");
            Console.WriteLine($"Keyboard stock in products.json: {await ReadKeyboardStockAsync(ct)}");

            Console.WriteLine("\n===== Race condition demo: SAFE reservation =====");
            await SetKeyboardStockAsync(1, ct);

            var safe10 = _products.ReserveStockAsync(4, 1, ct);
            var safe11 = _products.ReserveStockAsync(4, 1, ct);
            bool safeResult10 = await safe10;
            bool safeResult11 = await safe11;

            Console.WriteLine($"Order #10 got the keyboard: {safeResult10}");
            Console.WriteLine($"Order #11 got the keyboard: {safeResult11}");
            Console.WriteLine($"Keyboard stock in products.json: {await ReadKeyboardStockAsync(ct)}");
        }

        private async Task SetKeyboardStockAsync(int stock, CancellationToken ct)
        {
            await _store.UpdateAsync<Product>(DataFiles.Products,
                products => products.First(p => p.Id == 4).StockQuantity = stock, ct);
        }

        private async Task<int> ReadKeyboardStockAsync(CancellationToken ct)
        {
            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
            return products.First(p => p.Id == 4).StockQuantity;
        }
    }
}
