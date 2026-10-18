namespace InvoiceExport;

public sealed class OrderDatabase
{
    readonly List<Order> _pending =
    [
        new(1001, 1, 240.00m, "Zooba"),
        new(1002, 2, 180.50m, "Koshary El Tahrir"),
        new(1003, 3, 95.00m, "Hawawshi House"),
        new(1004, 1, 310.00m, "Zooba"),
        new(1005, 4, 150.00m, "Fattah Street"),
    ];

    public async Task<List<Order>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        await Task.Delay(800, ct);
        //Thread.Sleep(800);
        return _pending.ToList();
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct=default)
    {
        await Task.Delay(400, ct);
        //Thread.Sleep(400);
        return _pending.FirstOrDefault(o => o.Id == id);
    }
}
