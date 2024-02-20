using Google.Api.Gax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img
{
    public class Logger
    {
        private static readonly object _lock = new();

        public static void LogSuccess(string message)
        {
            lock (_lock)
            {
                Console.WriteLine($"[{DateTime.Now}]{"\u001b[32m"}Success! {"\u001b[0m"}{message}");
                Debug.WriteLine(message);
            }
        }
        public static void LogError(string message)
        {
            lock (_lock)
            {
                Console.WriteLine($"[{DateTime.Now}]{"\u001b[31m"}Error! {"\u001b[0m"}{message}");
                Debug.WriteLine(message);
            }
        }
    }
}
