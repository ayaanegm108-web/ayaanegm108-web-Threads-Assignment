using OrderProcessing.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase2
{
//ai
    public static class BlockingDemo
    {
        public static void Run()
        {
            Console.WriteLine("\n===== .Result / .Wait() demo (the only allowed place) =====");

            // 1) .Result بيحجز الـ thread الحالي لحد ما الـ task تخلص
            Log.Info("Before .Result: this thread is about to block");
            var sw = Stopwatch.StartNew();

            int value = SlowAsync().Result;      // الـ thread واقف هنا ومش بيعمل أي حاجة

            sw.Stop();
            Log.Info($"After .Result: got {value} after {sw.ElapsedMilliseconds} ms, on the SAME thread (it was blocked the whole time)");

            // 2) الـ exception بيطلع ملفوف جوه AggregateException
            try
            {
                FailAsync().Wait();
            }
            catch (AggregateException ex)
            {
                Log.Error($".Wait() threw {ex.GetType().Name}; the real error is inside: \"{ex.InnerException?.Message}\"");
            }
        }

        private static async Task<int> SlowAsync()
        {
            await Task.Delay(1500);
            return 42;
        }

        private static async Task FailAsync()
        {
            await Task.Delay(300);
            throw new InvalidOperationException("Something failed inside the task");
        }
    }
}
