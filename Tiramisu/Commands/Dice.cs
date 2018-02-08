using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using NLog;

namespace Tiramisu.Commands
{
    internal class Dice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Command("dice"), Aliases("d")]
        [Description("Roll the dice!")]
        public async Task RollDice(CommandContext ctx, [Description("Max number or Min-Max number pair. (Default: 1-6)")]params int[] list)
        {
            Log.Info($"Check dice command - {ctx.Message.Content}");

            await ctx.TriggerTypingAsync();

            int minNum = 1, maxNum = 6;

            switch (list.Length)
            {
                case 0:
                    break;
                case 1:
                    maxNum = list[0];
                    break;
                case 2:
                    minNum = list[0];
                    maxNum = list[1];
                    break;
                default:
                    await ctx.RespondAsync("More than 3 parameters are not allowed.");
                    return;
            }

            var rd = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var result = rd.Next(minNum, maxNum);

            await ctx.RespondAsync($"{result}");
        }
    }
}
