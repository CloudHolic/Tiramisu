using NLog;

namespace Tiramisu
{
    internal static class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static void Main()
        {
            using (var bot = new Bot())
            {
                Log.Info($"Tiramisu bot started.");
                bot.RunAsync().Wait();
            }
        }
    }
}
