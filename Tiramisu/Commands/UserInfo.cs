using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using NLog;
using Tiramisu.Processors;
using Tiramisu.Structures;

namespace Tiramisu.Commands
{
    internal class UserInfo
    {
        private readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Command("user"), Aliases("u")]
        [Description("Get the specified user info.")]
        public async Task User(CommandContext ctx, [Description("User name in Osu")] string name, [Description("Specified mode to get user info.")] string mode = null)
        {
            Log.Info($"Check rate command - {ctx.Message.Content}");

            await ctx.TriggerTypingAsync();

            if (mode == null)
                mode = "s";

            var osuMode = OsuModeExtensions.ModeParse(mode);
            if (osuMode == OsuMode.Unknown)
            {
                await ctx.RespondAsync($"Unknown mode {mode}");
                return;
            }

            try
            {
                var apiResult = await OsuService.UserInfoAsync(name, (int) osuMode);
                if (apiResult.Count == 0)
                {
                    await ctx.RespondAsync($"Error while getting user {name}'s info.");
                    return;
                }

                var resultString =
                    $"User ID: {apiResult[0].UserId}\n" +
                    $"User Name: {apiResult[0].UserName}\n" +
                    $"Count300: {apiResult[0].Count300}\n" +
                    $"Count100: {apiResult[0].Count100}\n" +
                    $"Count50: {apiResult[0].Count50}\n" +
                    $"Play Count: {apiResult[0].PlayCount}\n" +
                    $"Ranked Score: {apiResult[0].RankedScore}\n" +
                    $"Total Score: {apiResult[0].TotalScore}\n" +
                    $"PP Rank: {apiResult[0].PpRank}\n" +
                    $"Level: {apiResult[0].Level}\n" +
                    $"PP: {apiResult[0].RawPp}\n" +
                    $"Accuracy: {apiResult[0].Accuracy}\n" +
                    $"Count Rank SS: {apiResult[0].CountRankSs}\n" +
                    $"Count Rank SSH: {apiResult[0].CoundRankSsh}\n" +
                    $"Count Rank S: {apiResult[0].CountRankS}\n" +
                    $"Count Rank SH: {apiResult[0].CountRankSh}\n" +
                    $"Count Rank A: {apiResult[0].CountRankA}\n" +
                    $"Country: {apiResult[0].Country}\n" +
                    $"PP Country Rank: {apiResult[0].PpCountryRank}";
                await ctx.RespondAsync(resultString);
            }
            catch (Exception e)
            {
                await ctx.RespondAsync("Error while working... Please try again.");
                Log.Error(e, "An error occurred.");
            }
        }
    }
}
