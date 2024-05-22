using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class TexCommand
{
    public static readonly Texture2D Hysteresis = ContentFinder<Texture2D>.Get("UI/Designators/FarmingHysteresis_Hysteresis");
}
