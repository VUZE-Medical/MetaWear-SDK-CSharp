using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class Program {
        static async Task Main(string[] args) 
        {
            await MainAsync(args);
        }

        private static async Task MainAsync(string[] args) {
            var type = Type.GetType(args[0]);
            await (Task) type.GetMethod("RunAsync", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { args.TakeLast(args.Length - 1).ToArray() });
        }
    }
}