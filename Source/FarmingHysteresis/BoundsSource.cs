namespace FarmingHysteresis;

/// <summary>
/// Determines which storage tier a plant grower's hysteresis bounds are read from.
/// </summary>
internal enum BoundsSource
{
    /// <summary>The grower's own bounds, not shared with anything else.</summary>
    Self,

    /// <summary>Bounds shared with every other grower of the same harvest type on the same map.</summary>
    Map,

    /// <summary>Bounds shared with every other grower of the same harvest type across the whole save.</summary>
    Game,
}

/// <summary>
/// Migration helpers for the pre-<see cref="BoundsSource"/> boolean "use global values" scheme.
/// </summary>
internal static class BoundsSourceMigration
{
    /// <summary>
    /// Maps an old <c>useGlobalValues</c> boolean to the equivalent <see cref="BoundsSource"/>.
    /// </summary>
    /// <param name="oldValue">The old boolean value; <see langword="true"/> meant "use the
    /// map-scoped shared bounds," which is now the <see cref="BoundsSource.Map"/> tier.</param>
    internal static BoundsSource FromOldUseGlobalValues(bool oldValue) =>
        oldValue ? BoundsSource.Map : BoundsSource.Self;
}
