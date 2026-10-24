using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class SyncCustomerRepository
    {
        private readonly JsonFileStore _store;

        public SyncCustomerRepository(JsonFileStore store) => _store = store;

        public Customer? GetById(int id)
        {
            Log.Info("SyncCustomerRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"SyncCustomerRepository -> Executing query for Customer #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var customer = _store.ReadAll<Customer>(DataFiles.Customers)
                                 .FirstOrDefault(c => c.Id == id);

            if (customer is null)
                Log.Info($"SyncCustomerRepository -> Customer #{id} not found");
            else
                Log.Success($"SyncCustomerRepository -> Customer #{id} found ({customer.Name})");

            return customer;
        }
    }
}
