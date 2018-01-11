using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Tiramisu.Entities;

namespace Tiramisu.Commands
{
    [Group("interactivity"), Aliases("i")]
    internal class Interactivity
    {
        private const string ConfirmRegex = "\\b[Yy][Ee]?[Ss]?\\b|\\b[Nn][Oo]?\\b";
        private const string YesRegex = "[Yy][Ee]?[Ss]?";
        private const string NoRegex = "[Nn][Oo]?";

        private readonly Dependencies _dep;
        public Interactivity(Dependencies d)
        {
            _dep = d;
        }

        [Command("confirmation")]
        public async Task ConfirmationAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("Are you sure?");
            var m = await _dep.Interactivity.WaitForMessageAsync(
                x => x.Channel.Id == ctx.Channel.Id
                     && x.Author.Id == ctx.Member.Id
                     && Regex.IsMatch(x.Content, ConfirmRegex));

            if (Regex.IsMatch(m.Message.Content, YesRegex))
                await ctx.RespondAsync("Confirmation Received");
            else
                await ctx.RespondAsync("Confirmation Denied");
        }
    }
}