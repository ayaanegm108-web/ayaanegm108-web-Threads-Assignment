using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Logging
{
    public class Log
    {
        private static readonly object _sync = new();
        private static readonly ConcurrentDictionary<int, byte> _threads = new();

        public static int DistinctThreads => _threads.Count;
        public static void ResetThreadTracking() => _threads.Clear();

        public static void Info(string message) => Write(message, null);
        public static void Success(string message) => Write(message, ConsoleColor.Green);
        public static void Error(string message) => Write(message, ConsoleColor.Red);

        private static void Write(string message, ConsoleColor? color)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            int threadId = Environment.CurrentManagedThreadId;
            _threads.TryAdd(threadId, 0);

            string line = $"[{time}] [Thread {threadId}] {message}";
            lock (_sync)
            {
                if (color.HasValue)
                    Console.ForegroundColor = color.Value;

                Console.WriteLine(line);

                Console.ResetColor();
            }
        }
    }
}
