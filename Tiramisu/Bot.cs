using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Tiramisu.Entities;
using NLog;
using OsuParser.Exceptions;

namespace Tiramisu
{
    public class Bot : IDisposable
    {
        private readonly DiscordClient _client;
        private readonly StartTimes _startTimes;
        private readonly CancellationTokenSource _cts;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private InteractivityModule _interactivity;
        private CommandsNextModule _cnext;
        private Config _config;

        public Bot()
        {
            if (!File.Exists("config.json"))
            {
                new Config().SaveToFile("config.json");
                #region !! Report to user that config has not been set yet !! (aesthetics)
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                WriteCenter("▒▒▒▒▒▒▒▒▒▄▄▄▄▒▒▒▒▒▒▒", 2);
                WriteCenter("▒▒▒▒▒▒▄▀▀▓▓▓▀█▒▒▒▒▒▒");
                WriteCenter("▒▒▒▒▄▀▓▓▄██████▄▒▒▒▒");
                WriteCenter("▒▒▒▄█▄█▀░░▄░▄░█▀▒▒▒▒");
                WriteCenter("▒▒▄▀░██▄░░▀░▀░▀▄▒▒▒▒");
                WriteCenter("▒▒▀▄░░▀░▄█▄▄░░▄█▄▒▒▒");
                WriteCenter("▒▒▒▒▀█▄▄░░▀▀▀█▀▒▒▒▒▒");
                WriteCenter("▒▒▒▄▀▓▓▓▀██▀▀█▄▀▀▄▒▒");
                WriteCenter("▒▒█▓▓▄▀▀▀▄█▄▓▓▀█░█▒▒");
                WriteCenter("▒▒▀▄█░░░░░█▀▀▄▄▀█▒▒▒");
                WriteCenter("▒▒▒▄▀▀▄▄▄██▄▄█▀▓▓█▒▒");
                WriteCenter("▒▒█▀▓█████████▓▓▓█▒▒");
                WriteCenter("▒▒█▓▓██▀▀▀▒▒▒▀▄▄█▀▒▒");
                WriteCenter("▒▒▒▀▀▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒");
                Console.BackgroundColor = ConsoleColor.Yellow;
                WriteCenter("WARNING", 3);
                Console.ResetColor();
                WriteCenter("Thank you Mario!", 1);
                WriteCenter("But our config.json is in another castle!");
                WriteCenter("(Please fill in the config.json that was generated.)", 2);
                WriteCenter("Press any key to exit..", 1);
                Console.SetCursorPosition(0, 0);
                Console.ReadKey();
                #endregion
                Environment.Exit(0);
            }
            _config = Config.LoadFromFile("config.json");
            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                EnableCompression = true,
                Token = _config.Token,
                TokenType = TokenType.Bot,
            });

            _interactivity = _client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = TimeoutBehaviour.Delete,
                PaginationTimeout = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(30)
            });

            _startTimes = new StartTimes
            {
                BotStart = DateTime.Now,
                SocketStart = DateTime.MinValue
            };

            _cts = new CancellationTokenSource();

            DependencyCollection dep = null;
            using (var d = new DependencyCollectionBuilder())
            {
                d.AddInstance(new Dependencies
                {
                    Interactivity = _interactivity,
                    StartTimes = _startTimes,
                    Cts = _cts
                });
                dep = d.Build();
            }

            _cnext = _client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDefaultHelp = true,
                EnableDms = true,
                EnableMentionPrefix = true,
                StringPrefix = _config.Prefix,
                Dependencies = dep
            });
            
            _cnext.RegisterCommands<Commands.RateChange>();
            _cnext.RegisterCommands<Commands.UserInfo>();

            // Hook some events for logging.
            _client.Ready += OnReadyAsync;
            _client.ClientErrored += ClientError;
            _cnext.CommandExecuted += CommandExecuted;
            _cnext.CommandErrored += CommandErroredAsync;
        }

        public async Task RunAsync()
        {
            await _client.ConnectAsync();
            await WaitForCancellationAsync();
        }

        private async Task WaitForCancellationAsync()
        {
            while (!_cts.IsCancellationRequested)
                await Task.Delay(-1);
        }

        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            Log.Info("Tiramisu bot is now ready!");

            await Task.Yield();
            _startTimes.SocketStart = DateTime.Now;
        }

        private static Task ClientError(ClientErrorEventArgs e)
        {
            Log.Error(e.Exception, "ClientError Event fired. - An error occurred.");

            return Task.CompletedTask;
        }

        private static Task CommandExecuted(CommandExecutionEventArgs e)
        {
            Log.Info($"Command {e.Command.Name} executed in Channel {e.Context.Channel.Name} in Guild {e.Context.Guild.Name} written by {e.Context.Message.Author}.");

            return Task.CompletedTask;
        }

        private static async Task CommandErroredAsync(CommandErrorEventArgs e)
        {
            Log.Error(e.Exception,
                $"Command {e.Command.Name} errored in Channel {e.Context.Channel.Name} in Guild {e.Context.Guild.Name} written by {e.Context.Message.Author}.");

            if (e.Exception is InvalidBeatmapException)
            {
                await e.Context.RespondAsync("It's an invalid beatmap.");
            }
        }

        public void Dispose()
        {
            _client.Dispose();
            _interactivity = null;
            _cnext = null;
            _config = null;
        }

        private static void WriteCenter(string value, int skipline = 0)
        {
            for (var i = 0; i < skipline; i++)
                Console.WriteLine();

            Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
            Console.WriteLine(value);
        }
    }
}
