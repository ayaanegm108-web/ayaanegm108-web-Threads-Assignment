using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Logging
{
    public class Log
    {
        private static readonly object _sync = new();

        public static void Info(string message) => Write(message, null);
        public static void Success(string message) => Write(message, ConsoleColor.Green);
        public static void Error(string message) => Write(message, ConsoleColor.Red);

        private static void Write(string message, ConsoleColor? color)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            int threadId = Environment.CurrentManagedThreadId;
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
