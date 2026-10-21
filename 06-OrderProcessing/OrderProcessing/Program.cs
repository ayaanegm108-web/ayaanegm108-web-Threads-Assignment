using OrderProcessing.Shared.Logging;

namespace OrderProcessing;

static class Program
{
    static async Task Main(string[] args)
    {
        int? cancelAfterMs = ParseCancelAfter(args);

    
        if (args.Length == 0 || args[0].StartsWith("--"))
        {
            Console.WriteLine("Usage: dotnet run -- phase1 | phase2 | phase3 | phase4 | all [--cancel-after <ms>]");
            return;
        }

        string choice = args[0].ToLowerInvariant();

        string[] phases = choice == "all"
            ? new[] { "phase1", "phase2", "phase3", "phase4" }
            : new[] { choice };

        foreach (var phase in phases)
        {
            bool cancelled = await RunPhaseAsync(phase, cancelAfterMs);
            if (cancelled) break;
        }
    }

    static int? ParseCancelAfter(string[] args)
    {
        int i = Array.IndexOf(args, "--cancel-after");
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int ms))
            return ms;
        return null;
    }


    static async Task<bool> RunPhaseAsync(string name, int? cancelAfterMs)
    {
        using var cts = new CancellationTokenSource();

        ConsoleCancelEventHandler handler = (_, e) =>
        {
            e.Cancel = true;  
            cts.Cancel();    
        };
        Console.CancelKeyPress += handler;

        try
        {
            if (cancelAfterMs.HasValue)
                cts.CancelAfter(cancelAfterMs.Value);

            switch (name)
            {
                case "phase1": /* await Phase1Runner.RunAsync(cts.Token); */ break;
                case "phase2": /* await Phase2Runner.RunAsync(cts.Token); */ break;
                case "phase3": /* await Phase3Runner.RunAsync(cts.Token); */ break;
                case "phase4": /* await Phase4Runner.RunAsync(cts.Token); */ break;
                default:
                    Console.WriteLine($"Unknown phase '{name}'.");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            Log.Error($"{name} cancelled");
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }

        return cts.IsCancellationRequested;
    }
}

//Console.WriteLine("Cause,I`m feeling Gooooooooood!!!!!");