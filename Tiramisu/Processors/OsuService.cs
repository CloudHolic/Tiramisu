using System.Collections.Generic;
using System.Threading.Tasks;
using Tiramisu.Entities;
using Tiramisu.RestApi;
using Tiramisu.Structures;

namespace Tiramisu.Processors
{
    public static class OsuService
    {
        public static async Task<List<OsuUserInfo>> UserInfoAsync(string userName, int mode)
        {
            var config = Config.LoadFromFile("config.json");
            var qp = new Dictionary<string, string>
            {
                {"k", config.OsuApiKey},
                {"u", userName},
                {"m", mode.ToString()},
                {"type", "string"},
                {"event_days", "1"}
            };

            return await RestClient.Instance.GetAsync<List<OsuUserInfo>>("get_user", qp);
        }
    }
}
