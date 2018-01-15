namespace Tiramisu.Structures
{
    public enum OsuMode
    {
        Standard,
        
        Taiko,
        
        CatchTheBeat,

        Mania,

        Unknown
    }

    internal static class OsuModeExtensions
    {
        internal static OsuMode ModeParse(string parseStr)
        {
            var lowercaseStr = parseStr.ToLower();
            switch (lowercaseStr)
            {
                case "o":
                case "s":
                    return OsuMode.Standard;
                case "t":
                    return OsuMode.Taiko;
                case "c":
                    return OsuMode.CatchTheBeat;
                case "m":
                    return OsuMode.Mania;
                default:
                    return OsuMode.Unknown;
            }
        }
    }
}
