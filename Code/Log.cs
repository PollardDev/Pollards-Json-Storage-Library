using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PollardsStorageSys.Log {
    public class Log {
        internal static void LogMessage(string message) {
            Console.WriteLine($"PollardStorageSys: [{DateTime.Now}] {message}");
        }
        internal static void LogError(string message) {
            Console.WriteLine($"PollardStorageSys: [{DateTime.Now}] ERROR: {message}");
        }
        internal static void LogWarning(string message) {
            Console.WriteLine($"PollardStorageSys: [{DateTime.Now}] WARNING: {message}");
        }
    }
}
