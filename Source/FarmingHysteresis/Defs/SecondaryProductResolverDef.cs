namespace FarmingHysteresis.Defs;

// Rimworld Defs have values set through reflection
#pragma warning disable CS8618

/// <summary>
/// Extension point letting a third-party integration teach Farming Hysteresis about a plant's
/// secondary harvested product (e.g. Vanilla Expanded Framework's dual-crop mechanic) without
/// this mod's own assemblies ever referencing that third party's types directly - mirrors the
/// `Type` field + lazy-instantiated worker idiom Colony Manager Redux itself uses for
/// <c>RecipeProductResolverDef</c> (see <c>Docs/CMRIntegrationRework.md</c>'s "CMR API grounding"
/// section). `DefDatabase&lt;SecondaryProductResolverDef&gt;.AllDefs` discovery is automatic
/// across mods with zero registration call needed. Consumed via
/// <see cref="SecondaryProductResolvers.ResolveFor"/>.
/// </summary>
public class SecondaryProductResolverDef : Def
{
    /// <summary>The <see cref="SecondaryProductResolverWorker"/> subclass to instantiate for this def.</summary>
    public Type resolverClass;

    /// <summary>The lazily-instantiated worker for this def.</summary>
    public SecondaryProductResolverWorker Worker =>
        field ??= (SecondaryProductResolverWorker)Activator.CreateInstance(resolverClass);
}

/// <summary>
/// Resolves whatever secondary product(s) a given plant can additionally yield, beyond its own
/// <c>plant.harvestedThingDef</c>. A plant with no such mechanism should simply yield nothing.
/// </summary>
public abstract class SecondaryProductResolverWorker
{
    /// <summary>Resolves the secondary product(s) <paramref name="plantDef"/> can yield, if any.</summary>
    public abstract IEnumerable<ThingDef> ResolveSecondaryProducts(ThingDef plantDef);
}

/// <summary>Aggregates every registered <see cref="SecondaryProductResolverDef"/>'s results for a given plant.</summary>
public static class SecondaryProductResolvers
{
    /// <summary>
    /// Every secondary product any registered resolver reports for <paramref name="plantDef"/>,
    /// deduplicated. Empty when <paramref name="plantDef"/> is <see langword="null"/> or no
    /// resolver mod is installed/active - callers never need to check for either case separately.
    /// </summary>
    public static IEnumerable<ThingDef> ResolveFor(ThingDef? plantDef) =>
        plantDef == null
            ? []
            : DefDatabase<SecondaryProductResolverDef>
                .AllDefsListForReading.SelectMany(d => d.Worker.ResolveSecondaryProducts(plantDef))
                .Distinct();
}
