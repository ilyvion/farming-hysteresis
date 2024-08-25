[StaticConstructorOnStartup]
internal static class Resources
{
    public static readonly Texture2D Hysteresis = ContentFinder<Texture2D>.Get("UI/Designators/FarmingHysteresis_Hysteresis");
    public static readonly Texture2D Stopwatch = ContentFinder<Texture2D>.Get("UI/Icons/FarmingHysteresis_StopWatch");

    public static readonly Color Orange = new(1f, 48f / 85f, 0f);
}
