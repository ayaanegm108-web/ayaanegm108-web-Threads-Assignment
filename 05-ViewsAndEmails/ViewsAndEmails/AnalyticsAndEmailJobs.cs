namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly Dictionary<string, int> Views = new();
    public readonly Queue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();
    private readonly object _lock = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
        lock (_lock)
        {
            if (!Views.ContainsKey(product))
                Views.Add(product, 0);

            Views[product] = Views[product] + 1;
        }
    }

    public void PlaceOrder(int orderId)
    {
        lock (_lock)
        {
            EmailJobs.Enqueue($"Email: order {orderId}");
        }
    }

    public void EmailWorker()
    {
        while (true)
        {
            try
            {
                string? job = null;
                lock (_sentSync)
                {
                    if (EmailJobs.Count > 0)
                        job = EmailJobs.Dequeue();

                    if (job is not null)
                    {

                        SentEmails.Add(job);
                        continue;
                    }

                    if (ProducersDone && EmailJobs.Count == 0)
                        return;

                    Thread.Sleep(1);
                }
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("InvalidOperationException");
                if (ProducersDone)
                    return;
            }
        }
    }
}
