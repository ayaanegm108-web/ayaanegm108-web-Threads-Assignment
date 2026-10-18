using System.Diagnostics;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }
    async Task<string> ExportOrderAsync(Order order, CancellationToken ct)
    {
        Order fresh = await _db.GetOrderByIdAsync(order.Id, ct)
            ?? throw new InvalidOperationException($"Order {order.Id} missing");

        CustomerProfile customer = await _customers.ReadProfileAsync(fresh.CustomerId, ct);
        return await _invoices.WriteInvoiceAsync(fresh, customer, ct);
    }

    public async Task ExportAllPendingAsync(CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        List<Order> orders = await _db.GetPendingOrdersAsync(ct);

        var tasks = new List<Task<string>>();
        foreach (Order order in orders)
            tasks.Add(ExportOrderAsync(order, ct));

        foreach (Task<string> task in tasks)
        {
            try
            {
                string path = await task;
                Console.WriteLine($"Wrote {path}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export failed: {ex.Message}");
            }
        }

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");
    }
}
