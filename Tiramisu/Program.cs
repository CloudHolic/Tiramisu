namespace Tiramisu
{
    internal static class Program
    {
        private static void Main()
        {
            using (var bot = new Bot())
            {
                bot.RunAsync().Wait();
            }
        }
    }
}
