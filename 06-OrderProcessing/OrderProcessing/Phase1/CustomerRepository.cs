using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class CustomerRepository
    {
        private readonly JsonFileStore _store;

        public CustomerRepository(JsonFileStore store) => _store = store;

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        {
            Log.Info("CustomerRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerRepository -> Executing query for Customer #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            var customers = await _store.ReadAllAsync<Customer>(DataFiles.Customers, ct);
            var customer = customers.FirstOrDefault(c => c.Id == id);

            if (customer is null)
                Log.Info($"CustomerRepository -> Customer #{id} not found");
            else
                Log.Success($"CustomerRepository -> Customer #{id} found ({customer.Name})");

            return customer;
        }
    }
}
