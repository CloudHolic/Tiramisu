﻿using System;
using System.Collections.Generic;
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
            var resultFiles = Tuple.Create(string.Empty, new List<string>());
            
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

                resultFiles = RateChangerThread.Instance.StartWorker(threadInfo);
                using (var fstream = File.Open(resultFiles.Item1, FileMode.Open))
                {
                    if(resultFiles.Item2.Count > 0)
                        await ctx.RespondAsync($"Excepted diffs: {string.Join(",", resultFiles.Item2)}");
                    await ctx.RespondWithFileAsync(fstream, Path.GetFileName(resultFiles.Item1), "Done!");
                }
            }
            catch (Exception e)
            {
                await ctx.RespondAsync("Error while working... Please try again.");
                Log.Error(e, "An error occurred.");
            }
            finally
            {
                File.Delete(resultFiles.Item1);
                Directory.Delete(Path.Combine(_config.FileDownloadPath, Path.GetFileNameWithoutExtension(fileName)), true);
                File.Delete(filePath);
                Log.Info("File delete completed.");
            }
        }
    }
}
