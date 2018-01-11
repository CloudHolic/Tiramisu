using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Tiramisu.Entities;

namespace Tiramisu.Commands
{
    [Group("owner"), Aliases("o"), RequireOwner, Hidden]
    internal class Owner
    {
        private readonly Dependencies _dep;
        public Owner(Dependencies d)
        {
            this._dep = d;
        }

        [Command("shutdown"), Hidden]
        public async Task ShutdownAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("Shutting down!");
            _dep.Cts.Cancel();
        }
    }
}