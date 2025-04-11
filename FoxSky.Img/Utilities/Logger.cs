using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img.Utilities
{
    public class Logger
    {
        public static void LogSuccess(string message)
        {
            try
            {
                Console.Write($"[{DateTime.Now}]");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" SUCCESS! ");
                Console.Write($"{message}");
                Debug.WriteLine(message);
                Console.WriteLine();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void LogError(string message)
        {
            try
            {
                Console.Write($"[{DateTime.Now}]");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" ERROR! ");
                Console.Write($"{message}");
                Debug.WriteLine(message);
                Console.WriteLine();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void LogWarning(string message)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("WARNING! ");
                Console.Write($"{message}");
                Debug.WriteLine(message);
                Console.WriteLine();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                Console.Write($"[{DateTime.Now}]");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(" INFO: ");
                Console.Write($"{message}");
                Debug.WriteLine(message);
                Console.WriteLine();
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
