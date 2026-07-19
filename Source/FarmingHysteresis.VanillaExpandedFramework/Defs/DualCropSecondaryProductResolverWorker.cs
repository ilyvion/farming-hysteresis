using FarmingHysteresis.Defs;
#if v1_6
using VEF.Plants;
#else
using VanillaPlantsExpanded;
#endif

namespace FarmingHysteresis.VanillaExpandedFramework.Defs;

/// <summary>
/// Resolves the secondary product(s) of a Vanilla Expanded Framework dual-crop plant (the
/// <c>DualCropExtension</c> <c>DefModExtension</c> - namespaced <c>VanillaPlantsExpanded</c>
/// through RimWorld 1.5, renamed <c>VEF.Plants</c> at 1.6, confirmed via decompiler; same field
/// shape both versions). A plant with no such extension simply yields nothing.
/// </summary>
/// <remarks>
/// For a <c>randomOutput</c> plant (one harvest randomly drops exactly one of
/// <c>randomSecondaryOutput</c>), this yields every listed candidate rather than guessing which
/// one - there's no way to know in advance which will actually drop, and Farming Hysteresis'
/// tracked-item filter already sums stock across every allowed def, so tracking the union is the
/// only sound choice.
/// </remarks>
internal sealed class DualCropSecondaryProductResolverWorker : SecondaryProductResolverWorker
{
    public override IEnumerable<ThingDef> ResolveSecondaryProducts(ThingDef plantDef)
    {
        var ext = plantDef.GetModExtension<DualCropExtension>();
        if (ext == null)
        {
            yield break;
        }

        if (ext.secondaryOutput != null)
        {
            yield return ext.secondaryOutput;
        }

        if (ext.randomSecondaryOutput != null)
        {
            foreach (var def in ext.randomSecondaryOutput)
            {
                yield return def;
            }
        }
    }
}
