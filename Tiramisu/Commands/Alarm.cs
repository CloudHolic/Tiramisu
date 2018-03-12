using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using NLog;

namespace Tiramisu.Commands
{
    internal class Alarm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Command("alarm"), Aliases("a")]
        [Description("Set an alarm.")]
        public async Task SetAlarm(CommandContext ctx, [Description("Minutes")]int min, [Description("Subject")]string subject, [Description("Participants")]params string[] participants)
        {
            Log.Info($"Check alarm command - {ctx.Message.Content}");

            await ctx.TriggerTypingAsync();

            var timer = new Timer
            {
                Interval = min * 60 * 1000,
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => TimerElapsed(ctx, subject, participants.ToList());
            timer.Start();

            await ctx.RespondAsync($"{ctx.Member.DisplayName} set a new alarm.");
        }

        private async void TimerElapsed(CommandContext ctx, string subject, List<string> participantList)
        {
            Log.Info($"Alarm executed - {ctx.Message.Content}");

            var idList = (from member in ctx.Guild.Members where participantList.Contains(member.DisplayName) select member.Id).ToList();
            var mention = $"{ctx.Member.DisplayName} set an alarm at {ctx.Message.Timestamp:t} to";
            foreach (var id in idList)
                mention += $" <@{id}>,";
            mention = mention.TrimEnd(',');
            await ctx.RespondAsync(mention + $"\n {subject}");
        }
    }
}
