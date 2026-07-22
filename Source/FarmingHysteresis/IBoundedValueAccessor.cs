namespace FarmingHysteresis;

internal interface IBoundedValueAccessor
{
    BoundValues BoundValueRaw { get; }

    /// <summary>
    /// Returns the current bound values without persisting anything: an already-materialized
    /// entry is returned by reference (so in-place edits still land in the backing store once
    /// <see cref="CommitBoundValue"/> is called), otherwise a detached default is returned that
    /// is not added to the backing store.
    /// </summary>
    BoundValues PeekBoundValue();

    /// <summary>
    /// Ensures <paramref name="value"/> (as previously returned by <see cref="PeekBoundValue"/>)
    /// is present in the backing store. Called only once the player actually edits a row, so
    /// merely viewing/listing values never materializes defaults for every def.
    /// </summary>
    void CommitBoundValue(BoundValues value);
}
