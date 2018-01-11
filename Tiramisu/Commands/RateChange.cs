using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Tiramisu.Entities;
using Tiramisu.Processors;
using Tiramisu.Util;
using NLog;

namespace Tiramisu.Commands
{
    internal class RateChange
    {
        private readonly Dependencies _dep;
        private readonly Config _config = Config.LoadFromFile("config.json");
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public RateChange(Dependencies dep)
        {
            _dep = dep;
        }

        [Command("rate"), Aliases("r")]
        [Description("Convert a given *.osz file with a specific rate.")]
        public async Task Rate(CommandContext ctx, [Description("Rate to convert")]double rate)
        {
            var fileName = string.Empty;
            var filePath = string.Empty;
            var resultFile = string.Empty;
            
            try
            {
                DiscordAttachment attach;

                if (ctx.Message.Attachments.Count > 0 &&
                    Path.GetExtension(ctx.Message.Attachments[0].FileName) == ".osz")
                    attach = ctx.Message.Attachments[0];
                else
                {
                    await ctx.RespondAsync("You should upload an *.osz file.");
                    var reply = await _dep.Interactivity.WaitForMessageAsync(
                        x => x.Channel.Id == ctx.Channel.Id
                             && x.Author.Id == ctx.Member.Id
                             && x.Attachments.Count > 0 && Path.GetExtension(x.Attachments[0].FileName) == ".osz");
                    attach = reply.Message.Attachments[0];
                }

                Log.Info($"Check rate command - {ctx.Message.Content} with file {attach.FileName}");
                await ctx.RespondAsync("Working... Please wait for a second.");
                await ctx.TriggerTypingAsync();

                fileName = attach.FileName.Replace("_", " ");
                var url = attach.Url;
                filePath = Path.Combine(_config.FileDownloadPath, fileName);

                using (var client = new WebClient())
                {
                    client.DownloadFile(url, filePath);
                }
                
                ZipUtil.ExtractToDirectory(filePath, Path.Combine(_config.FileDownloadPath, Path.GetFileNameWithoutExtension(fileName)), true);

                var threadInfo = new RateChangerThreadInput
                {
                    Path = Path.Combine(_config.FileDownloadPath, Path.GetFileNameWithoutExtension(fileName)),
                    OszChecked = true,
                    Rate = rate,
                    OutPutDir = _config.FileOutputPath
                };

                resultFile = RateChangerThread.Instance.StartWorker(threadInfo);
                using (var fstream = File.Open(resultFile, FileMode.Open))
                {
                    await ctx.RespondWithFileAsync(fstream, Path.GetFileName(resultFile), "Done!");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred.");
            }
            finally
            {
                File.Delete(resultFile);
                Directory.Delete(Path.Combine(_config.FileDownloadPath, Path.GetFileNameWithoutExtension(fileName)), true);
                File.Delete(filePath);
                Log.Info("File delete completed.");
            }
        }
    }
}
