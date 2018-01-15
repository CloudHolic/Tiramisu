using Newtonsoft.Json;

namespace Tiramisu.Structures
{
    public class OsuUserInfo
    {
        [JsonProperty("user_id")]
        public int UserId { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("count300")]
        public int Count300 { get; set; }

        [JsonProperty("count100")]
        public int Count100 { get; set; }

        [JsonProperty("count50")]
        public int Count50 { get; set; }

        [JsonProperty("playcount")]
        public int PlayCount { get; set; }

        [JsonProperty("ranked_score")]
        public long RankedScore { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("pp_rank")]
        public int PpRank { get; set; }

        [JsonProperty("level")]
        public double Level { get; set; }

        [JsonProperty("pp_raw")]
        public double RawPp { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("count_rank_ss")]
        public int CountRankSs { get; set; }

        [JsonProperty("count_rank_ssh")]
        public int CoundRankSsh { get; set; }

        [JsonProperty("count_rank_s")]
        public int CountRankS { get; set; }

        [JsonProperty("count_rank_sh")]
        public int CountRankSh { get; set; }

        [JsonProperty("count_rank_a")]
        public int CountRankA { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("pp_country_rank")]
        public int PpCountryRank { get; set; }
    }
}