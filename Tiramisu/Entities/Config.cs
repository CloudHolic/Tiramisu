using System.IO;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Tiramisu.Entities
{
    internal class Config
    {
        /// <summary>
        /// Your bot's token.
        /// </summary>
        [JsonProperty("token")]
        internal string Token = "NDAwNDc4ODg5MjQ1Mjc4MjE5.DTcO2A.3IwXcJINqeXw0C0mtOl71YE2FMU";

        /// <summary>
        /// Your bot's prefix
        /// </summary>
        [JsonProperty("prefix")]
        internal string Prefix = "!";
        
        /// <summary>
        /// *.osz file download path.
        /// </summary>
        [JsonProperty("download")]
        internal string FileDownloadPath = "C:/DiscordBot/Tiramisu/Downloads";

        /// <summary>
        /// Result *.osz file output path.
        /// </summary>
        [JsonProperty("output")]
        internal string FileOutputPath = "C:/DiscordBot/Tiramisu/Outputs";

        /// <summary>
        /// Your favourite color.
        /// </summary>
        [JsonProperty("color")]
        private string _color = "#7289DA";

        /// <summary>
        /// Your favourite color exposed as a DiscordColor object.
        /// </summary>
        internal DiscordColor Color => new DiscordColor(_color);

        /// <summary>
        /// Loads config from a JSON file.
        /// </summary>
        /// <param name="path">Path to your config file.</param>
        /// <returns></returns>
        public static Config LoadFromFile(string path)
        {
            using (var sr = new StreamReader(path))
            {
                return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
            }
        }

        /// <summary>
        /// Saves config to a JSON file.
        /// </summary>
        /// <param name="path"></param>
        public void SaveToFile(string path)
        {
            using (var sw = new StreamWriter(path))
            {
                sw.Write(JsonConvert.SerializeObject(this));
            }
        }
    }
}