using ColonyManagerRedux;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// This job's own upper/lower bound pair and latch state — the CMR-side replacement for
/// <see cref="FarmingHysteresisData"/>'s per-grower bookkeeping (see
/// <c>Docs/CMRIntegrationRework.md</c>, "Why <c>Trigger_Threshold</c> doesn't fit"). The
/// tracked product is configured independently of what the job's growers actually harvest (see
/// <c>Docs/CMRIntegrationRework.md</c>, Step 4 — resolves #16, e.g. sowing Hops based on Beer
/// stock) via <see cref="TrackedThingFilter"/>, the same <see cref="ThingFilter"/>-based shape
/// CMR's own <see cref="Trigger_Threshold"/> uses, reusing its public
/// <c>Utilities.CountProducts</c>/<c>Utilities.CountProductsCoroutine</c> helpers instead of this
/// mod's own per-grower counting. <see cref="TrackedThingFilter"/> and friends
/// (<see cref="TrackedFilterFollowsTargetPlant"/>, <see cref="Stockpile"/>,
/// <see cref="CountAllOnMap"/>) all delegate to the job's currently active
/// <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/> entry, since what determines "this
/// crop is done" often differs per crop (Step 5 follow-up) - by default
/// (<see cref="CropRotationEntry.TrackedFilterFollowsTargetPlant"/>) each entry's filter tracks
/// whatever plant it's set to grow, matching this integration's original, simpler behavior
/// verbatim, until a player explicitly detaches it to track something else instead.
/// </summary>
internal sealed class Trigger_Hysteresis(ManagerJob job) : Trigger(job)
{
    private const float ProductIconSize = 24f;
    private const float ProductRowPadding = 5f;

    /// <summary>
    /// The active <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/> entry, or
    /// <see langword="null"/> if the job has no rotation entries yet (a brand new job with no crop
    /// picked). All of this trigger's per-crop state below is only ever read/written once an entry
    /// exists - see each member's own doc comment.
    /// </summary>
    private CropRotationEntry? ActiveEntry => HysteresisJob.ActiveEntry;

    /// <summary>
    /// The active rotation entry's lower bound. Only valid while <see cref="ActiveEntry"/> exists
    /// - see <c>Docs/CMRIntegrationRework.md</c>, Step 5.
    /// </summary>
    public int Lower
    {
        get => ActiveEntry!.Lower;
        set => ActiveEntry!.Lower = value;
    }

    /// <summary>See <see cref="Lower"/> - same shape, for the upper bound.</summary>
    public int Upper
    {
        get => ActiveEntry!.Upper;
        set => ActiveEntry!.Upper = value;
    }

    /// <summary>
    /// The active rotation entry's own hysteresis latch. Since each entry tracks its own latch
    /// independently (see <see cref="CropRotationEntry.LatchModeValue"/>), this is purely a
    /// read-through convenience for this trigger's own <see cref="State"/>/
    /// <see cref="StatusTooltip"/>/history consumers, which only ever care about whichever entry
    /// is currently active.
    /// </summary>
    internal LatchMode LatchModeValue
    {
        get => ActiveEntry!.LatchModeValue;
        private set => ActiveEntry!.LatchModeValue = value;
    }

    /// <summary>See <see cref="LatchModeValue"/> - same shape, for the active entry's tracked-thing count.</summary>
    internal int TrackedThingCount
    {
        get => ActiveEntry!.TrackedThingCount;
        private set => ActiveEntry!.TrackedThingCount = value;
    }

    public static ThingFilter ParentFilter { get; } =
        ThingFilter.CreateOnlyEverStorableThingFilter();

    /// <summary>
    /// The active rotation entry's tracked items filter. Kept distinct from
    /// <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/> per Step 4 - see this class's own
    /// doc comment.
    /// </summary>
    public ThingFilter TrackedThingFilter => ActiveEntry!.TrackedThingFilter;

    /// <summary>
    /// Whether <see cref="TrackedThingFilter"/> is kept in sync with the active entry's own plant
    /// (see <see cref="SyncTrackedFilterToTargetPlant"/>) rather than left to the player's own
    /// choice. Delegates to the active <see cref="CropRotationEntry.TrackedFilterFollowsTargetPlant"/>.
    /// </summary>
    public bool TrackedFilterFollowsTargetPlant
    {
        get => ActiveEntry!.TrackedFilterFollowsTargetPlant;
        set => ActiveEntry!.TrackedFilterFollowsTargetPlant = value;
    }

    /// <summary>Restricts <see cref="TrackedThingFilter"/> counting to a single stockpile, mirroring <see cref="Trigger_Threshold.Stockpile"/>.</summary>
    public Zone_Stockpile? Stockpile
    {
        get => ActiveEntry!.Stockpile;
        set => ActiveEntry!.Stockpile = value;
    }

    public bool CountAllOnMap
    {
        get => ActiveEntry!.CountAllOnMap;
        set => ActiveEntry!.CountAllOnMap = value;
    }

    /// <summary>
    /// Pure "seed once" helper behind <see cref="SyncTrackedFilterToTargetPlant"/> - resets
    /// <paramref name="filter"/> to allow only <paramref name="def"/> (or nothing, if
    /// <see langword="null"/>). Split out as a static method taking a bare <see cref="ThingFilter"/>
    /// so it's unit-testable without a live job/trigger.
    /// </summary>
    internal static void SyncFilterToSingleDef(ThingFilter filter, ThingDef? def) =>
        SyncFilterToDefs(filter, def != null ? [def] : []);

    /// <summary>
    /// Pure "seed once" helper generalizing <see cref="SyncFilterToSingleDef"/> to an arbitrary
    /// set of defs - resets <paramref name="filter"/> to allow only <paramref name="defs"/> (a
    /// crop rotation entry uses this to track its primary harvested product, a resolved secondary
    /// product, or both - see <see cref="CropRotationEntry.SyncTrackedFilterToTargetPlant"/> and
    /// <c>Docs/CMRIntegrationRework.md</c>'s Step 6). Split out as a static method taking a bare
    /// <see cref="ThingFilter"/> so it's unit-testable without a live job/trigger.
    /// </summary>
    internal static void SyncFilterToDefs(ThingFilter filter, IEnumerable<ThingDef> defs)
    {
        filter.SetDisallowAll();
        foreach (var def in defs)
        {
            filter.SetAllow(def, true);
        }
    }

    /// <summary>
    /// Delegates to the active entry's own <see cref="CropRotationEntry.SyncTrackedFilterToTargetPlant"/>.
    /// </summary>
    internal void SyncTrackedFilterToTargetPlant() => ActiveEntry?.SyncTrackedFilterToTargetPlant();

    public ManagerJob_FarmingHysteresis HysteresisJob => (ManagerJob_FarmingHysteresis)Job;

    /// <summary>
    /// Pure transition table behind <see cref="State"/> — identical to
    /// <see cref="FarmingHysteresisData.UpdateLatchModeAndHandling"/>'s latch logic, ported
    /// rather than reinvented, but split out here so it's unit-testable without a live
    /// grower/job. See <c>Source/FarmingHysteresis/FarmingHysteresisData.cs</c> for the
    /// original.
    /// </summary>
    internal static LatchMode ComputeNextLatchMode(
        LatchMode current,
        int count,
        int lower,
        int upper
    ) =>
        count < lower ? LatchMode.BelowLowerBound
        : count > upper ? LatchMode.AboveUpperBound
        : current switch
        {
            LatchMode.BelowLowerBound or LatchMode.Unknown => count > lower
                ? LatchMode.BetweenBoundsEnabled
                : current,
            LatchMode.AboveUpperBound => count < upper ? LatchMode.BetweenBoundsDisabled : current,
            LatchMode.BetweenBoundsEnabled or LatchMode.BetweenBoundsDisabled => current,
            _ => current,
        };

    /// <summary>
    /// Pure decision behind <see cref="ManagerJob_FarmingHysteresis.ComputeRoundRobinActiveEntryId"/>'s
    /// crop-rotation advance (see <c>Docs/CMRIntegrationRework.md</c>, Step 5 - resolves #6): the active crop's own upper
    /// bound was just reached (a fresh transition, not merely still sitting there from an earlier
    /// cycle) and there's actually more than one crop to rotate through. With
    /// <paramref name="rotationEntryCount"/> &lt;= 1 this is always <see langword="false"/> -
    /// today's exact single-crop behavior (sit disabled once over <c>Upper</c>, indefinitely) is
    /// unreachable/unaffected.
    /// </summary>
    internal static bool ShouldAdvanceRotation(
        LatchMode previous,
        LatchMode current,
        int rotationEntryCount
    ) =>
        rotationEntryCount > 1
        && previous != LatchMode.AboveUpperBound
        && current == LatchMode.AboveUpperBound;

    /// <summary>
    /// The gather phase's pure verdict on this cycle's latch/rotation update - counts things (a
    /// read, not a game-world change) but writes nothing; see <see cref="ComputeCycleUpdate"/>
    /// (produces this) and <see cref="ApplyCycleUpdate"/> (the only place it's ever written back).
    /// </summary>
    internal readonly struct CycleUpdate
    {
        public required IReadOnlyList<(
            int Id,
            int Count,
            LatchMode Latch
        )> EntryUpdates { get; init; }
        public required int? NewActiveEntryId { get; init; }
    }

    /// <summary>
    /// Computes what this cycle's latch/rotation update would be - every rotation entry's own
    /// <see cref="CropRotationEntry.TrackedThingCount"/>/<see cref="CropRotationEntry.LatchModeValue"/>
    /// (not just the active one's, since each crop's hysteresis is its own independent memory
    /// regardless of whether it's being actively grown right now - see
    /// <see cref="CropRotationEntry.LatchModeValue"/>'s own doc comment) and the new active entry
    /// according to <see cref="ManagerJob_FarmingHysteresis.Mode"/> (see
    /// <see cref="ManagerJob_FarmingHysteresis.ComputeNewActiveEntryId"/>) - <b>without writing any
    /// of it back</b>. Deliberately pure/side-effect-free: this is called from
    /// <see cref="ManagerJob_FarmingHysteresis.GatherJobDataCoroutine"/>, which per the base
    /// <c>ManagerJob&lt;TWorkData&gt;</c>'s own contract must not change anything in the game (see
    /// <c>Docs/CMRIntegrationRework.md</c>'s Step 5 follow-up) - the actual write only happens once
    /// this cycle's result reaches <see cref="ApplyCycleUpdate"/>, from
    /// <see cref="ManagerJob_FarmingHysteresis.ExecuteJobDataCoroutine"/>.
    /// </summary>
    internal CycleUpdate ComputeCycleUpdate()
    {
        var rotationEntries = HysteresisJob.RotationEntries;
        var activeEntryId = HysteresisJob.ActiveEntryId;
        LatchMode? previousActiveLatch = null;
        var entryUpdates = new List<(int Id, int Count, LatchMode Latch)>(rotationEntries.Count);

        foreach (var entry in rotationEntries)
        {
            var count = HysteresisJob.Manager.map.CountProducts(
                entry.TrackedThingFilter,
                entry.Stockpile,
                entry.CountAllOnMap
            );

            if (entry.Id == activeEntryId)
            {
                previousActiveLatch = entry.LatchModeValue;
            }

            var latch = ComputeNextLatchMode(entry.LatchModeValue, count, entry.Lower, entry.Upper);
            entryUpdates.Add((entry.Id, count, latch));
        }

        var newActiveEntryId = ManagerJob_FarmingHysteresis.ComputeNewActiveEntryId(
            HysteresisJob.Mode,
            [.. entryUpdates.Select(u => (u.Id, u.Latch))],
            activeEntryId,
            previousActiveLatch
        );

        return new CycleUpdate { EntryUpdates = entryUpdates, NewActiveEntryId = newActiveEntryId };
    }

    /// <summary>
    /// Writes back a <see cref="CycleUpdate"/> computed by <see cref="ComputeCycleUpdate"/> -
    /// the only place this trigger's/its entries' latch state or
    /// <see cref="ManagerJob_FarmingHysteresis.ActiveEntryId"/> is ever actually mutated. Called
    /// exactly once per actual manager job cycle, from
    /// <see cref="ManagerJob_FarmingHysteresis.ExecuteJobDataCoroutine"/> - deliberately NOT wired
    /// to any cached/on-demand read of <see cref="State"/>, so a rotation only ever advances - and
    /// sow/harvest state only ever changes - when a manager pawn actually works this job, not
    /// merely because a player is watching a progress bar or editing config while paused (see
    /// <c>Docs/CMRIntegrationRework.md</c>'s Step 5 follow-up).
    /// </summary>
    internal void ApplyCycleUpdate(CycleUpdate update)
    {
        foreach (var (id, count, latch) in update.EntryUpdates)
        {
            var entry = HysteresisJob.RotationEntries.First(e => e.Id == id);
            entry.TrackedThingCount = count;
            entry.LatchModeValue = latch;
        }

        HysteresisJob.ActiveEntryId = update.NewActiveEntryId;
    }

    /// <summary>
    /// Purely reflects <see cref="LatchModeValue"/> as of the last manager job cycle (see
    /// <see cref="ApplyCycleUpdate"/>) - reading this never recomputes or advances anything.
    /// </summary>
    /// <inheritdoc/>
    public override bool State =>
        LatchModeValue is LatchMode.BelowLowerBound or LatchMode.BetweenBoundsEnabled;

    /// <summary>
    /// A short human-readable description of <paramref name="latchMode"/> - the same
    /// "Current state: …" text this trigger's <see cref="DrawTriggerConfig"/> already showed for
    /// the job as a whole, split out as a static helper so <c>ManagerTab_FarmingHysteresis</c> can
    /// show each rotation entry's own hysteresis state (see
    /// <see cref="CropRotationEntry.LatchModeValue"/>) the same way, matching the legacy per-grower
    /// ITab's own status line.
    /// </summary>
    /// <remarks>
    /// <see cref="LatchMode.Unknown"/> gets its own CMR-specific wording
    /// (<c>FarmingHysteresis.CMR.LatchModeDesc.Unknown</c>) rather than reusing the legacy ITab's
    /// <c>FarmingHysteresis.LatchModeDesc.Unknown</c>, which calls it a bug: for a per-grower
    /// zone that's true (a live zone updates its latch every tick), but here it's the normal,
    /// expected state until the manager job's first cycle runs (see
    /// <see cref="ApplyCycleUpdate"/>), which can be an arbitrary delay after the job/entry is
    /// created.
    /// </remarks>
    internal static string DescribeLatchMode(LatchMode latchMode) =>
        "FarmingHysteresis.LatchModeDesc".Translate(
            latchMode == LatchMode.Unknown
                ? "FarmingHysteresis.CMR.LatchModeDesc.Unknown".Translate()
                : ("FarmingHysteresis.LatchModeDesc." + latchMode).Translate(
                    FarmingHysteresisMod.Settings.HysteresisMode.AsString()
                )
        );

    /// <summary>
    /// Like <see cref="State"/>, purely reflects the last manager job cycle rather than forcing a
    /// fresh computation - a brand new job (or one waiting on its next cycle) simply shows
    /// whatever it last computed (nothing yet, for a job that's never run).
    /// </summary>
    public override string StatusTooltip
    {
        get
        {
            var targetPlantDef = HysteresisJob.TargetPlantDef;
            return targetPlantDef == null
                ? "FarmingHysteresis.CMR.Trigger.NoTargetPlant".Translate()
                : "FarmingHysteresis.CMR.Trigger.Status".Translate(
                    TrackedThingCount,
                    TrackedCountNoun,
                    Lower,
                    Upper
                );
        }
    }

    /// <summary>
    /// Pure display-count-noun logic behind <see cref="TrackedCountNoun"/>, split out (same
    /// reasoning as <see cref="DescribeTrackedFilter"/>) so <c>ManagerTab_FarmingHysteresis</c>
    /// can describe each rotation entry's own tracked count the same way.
    /// </summary>
    internal static string DescribeTrackedCountNoun(ThingFilter filter)
    {
        var allowedDefs = filter.AllowedThingDefs.ToList();
        return allowedDefs.Count == 1
            ? allowedDefs[0].label
            : "FarmingHysteresis.CMR.Trigger.TrackedCountNoun.Generic".Translate();
    }

    /// <summary>
    /// A short label describing whatever <paramref name="filter"/> currently allows - the single
    /// allowed def's own label if there's exactly one (the common case, whether following the
    /// target plant or a player's own single-def choice), or a translated summary otherwise. Kept
    /// as a static, parameterized helper (rather than an instance property tied to this trigger's
    /// own <see cref="TrackedThingFilter"/>) so <c>ManagerTab_FarmingHysteresis</c>'s per-rotation-
    /// entry rows can describe each entry's own <see cref="CropRotationEntry.TrackedThingFilter"/>
    /// the same way, now that tracked items live per entry rather than once per job.
    /// </summary>
    internal static string DescribeTrackedFilter(ThingFilter filter)
    {
        var allowedDefs = filter.AllowedThingDefs.ToList();
        return allowedDefs.Count switch
        {
            0 => "FarmingHysteresis.CMR.Trigger.TrackedSummary.None".Translate(),
            1 => allowedDefs[0].label,
            _ => "FarmingHysteresis.CMR.Trigger.TrackedSummary.Multiple".Translate(
                allowedDefs.Count
            ),
        };
    }

    /// <summary>
    /// A plain, pluralizable noun phrase for <see cref="TrackedThingFilter"/>'s current contents -
    /// the single allowed def's own label if there's exactly one, or the generic "items" otherwise
    /// (whether zero or several defs are allowed). Unlike <see cref="DescribeTrackedFilter"/>, this always
    /// reads correctly when substituted into a "{count} {noun} in storage" sentence, and always
    /// refers unambiguously to <see cref="TrackedThingCount"/>'s combined total rather than a
    /// per-kind amount - the exact set of tracked defs is what "Configure tracked items…" and the
    /// tracked-product row are for.
    /// </summary>
    private string TrackedCountNoun => DescribeTrackedCountNoun(TrackedThingFilter);

    /// <inheritdoc/>
    /// <remarks>
    /// Mirrors <see cref="Trigger_Threshold.DrawVerticalProgressBars"/>, but this trigger has
    /// two thresholds instead of one - the base bar (scaled/filled against <see cref="Upper"/>,
    /// same as <c>Trigger_Threshold</c> scales against its single target) draws the upper-bound
    /// mark for free, then <see cref="DrawBoundMark"/> adds the missing lower-bound mark on top.
    /// </remarks>
    public override void DrawVerticalProgressBars(Rect progressRect, bool active)
    {
        progressRect.xMin += progressRect.width - 10;
        DrawVerticalProgressBar(
            progressRect,
            TrackedThingCount,
            Upper,
            StatusTooltip,
            active,
            Resources.BarBackgroundActiveTexture
        );
        DrawBoundMark(progressRect, Lower, Upper, TrackedThingCount, vertical: true);
    }

    /// <inheritdoc/>
    /// <remarks>See <see cref="DrawVerticalProgressBars"/>.</remarks>
    public override void DrawHorizontalProgressBars(Rect progressRect, bool active)
    {
        progressRect.height = SmallIconSize;
        DrawHorizontalProgressBar(
            progressRect,
            TrackedThingCount,
            Upper,
            StatusTooltip,
            active,
            Resources.BarBackgroundActiveTexture
        );
        DrawBoundMark(progressRect, Lower, Upper, TrackedThingCount, vertical: false);
    }

    /// <summary>
    /// Draws the extra bound-mark line the base <c>DrawVerticalProgressBar</c>/
    /// <c>DrawHorizontalProgressBar</c> helpers don't already draw for us (they only mark
    /// <paramref name="maxValue"/>, which the caller already used as the bar's own scale/target).
    /// </summary>
    private static void DrawBoundMark(
        Rect progressRect,
        float value,
        float maxValue,
        float currentValue,
        bool vertical
    )
    {
        var barRect = progressRect.ContractedBy(2f);
        var barLength = vertical ? barRect.height : barRect.width;
        var markPosition = ComputeMarkPosition(value, maxValue, currentValue, barLength);

        if (vertical)
        {
            var markHeight = barRect.yMin + (barRect.height - markPosition);
            Widgets.DrawLineHorizontal(progressRect.xMin, markHeight, progressRect.width);
        }
        else
        {
            var markWidth = barRect.xMin + markPosition;
            Widgets.DrawLineVertical(markWidth, progressRect.yMin, progressRect.height);
        }
    }

    /// <summary>
    /// Pure position math behind <see cref="DrawBoundMark"/> - reimplements
    /// <c>Trigger.ComputeProgressBarMetrics</c>'s max/unit scaling locally, since that method is
    /// <see langword="internal"/> to CMR's core assembly and not visible here, and must stay in
    /// sync with the scale the base <c>DrawVerticalProgressBar</c>/<c>DrawHorizontalProgressBar</c>
    /// helpers use so both bound marks land on the same bar. Split out (rather than left inline
    /// in <see cref="DrawBoundMark"/>) so this scaling math is unit-testable without a live
    /// <c>Rect</c>/IMGUI draw call.
    /// </summary>
    internal static float ComputeMarkPosition(
        float value,
        float maxValue,
        float currentValue,
        float barLength
    )
    {
        var max = Math.Max(Math.Max((int)(maxValue * 1.2f), maxValue + 1), currentValue);
        var unit = barLength / max;
        return value * unit;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Required by the abstract <see cref="Trigger"/> base but not currently called by this mod's
    /// own UI - <see cref="ManagerTab_FarmingHysteresis"/> draws each rotation entry's own tracked-
    /// product row, storage count, and "Configure tracked items…" button inline in the crop
    /// rotation list (see <c>ManagerTab_FarmingHysteresis.DrawRotationEntryTrackedItems</c>) rather
    /// than a separate, job-wide trigger section, per <c>Docs/CMRIntegrationRework.md</c>'s Step 5
    /// follow-up.
    /// </remarks>
    public override void DrawTriggerConfig(
        ref Vector2 cur,
        float width,
        float entryHeight,
        string? label = null,
        string? tooltip = null,
        List<Designation>? targets = null,
        Action? onOpenFilterDetails = null,
        Func<Designation, string>? designationLabelGetter = null
    )
    {
        var targetPlantDef = HysteresisJob.TargetPlantDef;

        if (targetPlantDef == null)
        {
            DrawWrappedLabel(ref cur, width, entryHeight, StatusTooltip);
            return;
        }

        cur.y += DrawTrackedProductRow(cur, width);

        DrawWrappedLabel(
            ref cur,
            width,
            entryHeight,
            "FarmingHysteresis.InStorage".Translate(TrackedCountNoun, TrackedThingCount)
        );

        DrawWrappedLabel(ref cur, width, entryHeight, DescribeLatchMode(LatchModeValue));
    }

    /// <summary>
    /// Above this many allowed defs, <see cref="DrawTrackedProductRow(ThingFilter, Vector2, float)"/>
    /// falls back to <see cref="DescribeTrackedFilter"/>'s condensed "N kinds of items" summary
    /// instead of listing every def - a dual-crop entry tracking "Both" (2 defs) or a small
    /// hand-picked selection reads much better spelled out, but an arbitrary player-built filter
    /// covering dozens of defs would just be a wall of icon rows.
    /// </summary>
    private const int MaxExplicitlyListedProducts = 5;

    /// <summary>
    /// The tracked-product row: icon+label when <see cref="TrackedThingFilter"/> allows exactly
    /// one def (the common case - following the target plant, or a player's own single-def
    /// choice), or a plain summary label otherwise (see <see cref="DescribeTrackedFilter"/>) -
    /// the filter can no longer be assumed to always resolve to a single def once it's
    /// independent of <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/> (Step 4).
    /// </summary>
    private float DrawTrackedProductRow(Vector2 pos, float width) =>
        DrawTrackedProductRow(TrackedThingFilter, pos, width);

    /// <summary>
    /// Pure-parameter version of the tracked-product row, split out so
    /// <c>ManagerTab_FarmingHysteresis</c> can draw each rotation entry's own
    /// <see cref="CropRotationEntry.TrackedThingFilter"/> the same way this trigger draws its
    /// active entry's - icon+label for each allowed def when there are
    /// <see cref="MaxExplicitlyListedProducts"/> or fewer (e.g. a dual-crop entry's primary and
    /// secondary product, listed one on top of the other), or a plain summary label otherwise
    /// (see <see cref="DescribeTrackedFilter"/>).
    /// </summary>
    internal static float DrawTrackedProductRow(ThingFilter filter, Vector2 pos, float width)
    {
        var allowedDefs = filter.AllowedThingDefs.ToList();
        if (allowedDefs.Count is >= 1 and <= MaxExplicitlyListedProducts)
        {
            var start = pos;
            foreach (var def in allowedDefs)
            {
                pos.y += DrawProductRow(def, pos, width);
            }
            return pos.y - start.y;
        }

        var rowHeight = ProductIconSize + (2 * ProductRowPadding);
        var rect = new Rect(pos.x, pos.y, width, rowHeight);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect, DescribeTrackedFilter(filter));
        Text.Anchor = TextAnchor.UpperLeft;
        return rowHeight;
    }

    /// <summary>
    /// A label row sized to fit its (possibly multi-line) wrapped text, rather than the fixed
    /// <c>entryHeight</c> single-line rows used elsewhere - a fixed height clips longer messages
    /// (e.g. <see cref="StatusTooltip"/>'s no-target-plant message or the empty-bounds warning)
    /// instead of wrapping them legibly.
    /// </summary>
    private static void DrawWrappedLabel(ref Vector2 cur, float width, float minHeight, string text)
    {
        var height = Mathf.Max(minHeight, Text.CalcHeight(text, width));
        Widgets.Label(new Rect(cur.x, cur.y, width, height), text);
        cur.y += height;
    }

    /// <summary>
    /// Icon + label row for <paramref name="harvestedThingDef"/>, ported from the legacy
    /// <c>ITab_Hysteresis.DrawProductRow</c> so the CMR job's UI shows the tracked product the
    /// same way the old per-grower ITab did, rather than only mentioning it in a plain text line.
    /// </summary>
    private static float DrawProductRow(ThingDef harvestedThingDef, Vector2 pos, float width)
    {
        var rowHeight = ProductIconSize + (2 * ProductRowPadding);
        var rowRect = new Rect(pos.x, pos.y, width, rowHeight);
        var iconRect = new Rect(
            pos.x + ProductRowPadding,
            pos.y + ProductRowPadding,
            ProductIconSize,
            ProductIconSize
        );
        var labelRect = new Rect(
            iconRect.xMax + ProductRowPadding,
            pos.y,
            width - iconRect.width - (3 * ProductRowPadding),
            rowHeight
        );

        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        Widgets.DrawHighlightIfMouseover(rowRect);
        GUI.color = Color.white;

        GUI.DrawTexture(iconRect, harvestedThingDef.uiIcon);

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, harvestedThingDef.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;

        if (Mouse.IsOver(rowRect))
        {
            TipSignal tip = new(
                harvestedThingDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor)
                    + "\n\n"
                    + harvestedThingDef.description
            );
            TooltipHandler.TipRegion(rowRect, tip);
        }

        if (Widgets.ButtonInvisible(rowRect, doMouseoverSound: false))
        {
            Find.WindowStack.Add(new Dialog_InfoCard(harvestedThingDef));
        }

        return rowHeight;
    }

    /// <inheritdoc/>
    /// <summary>
    /// This trigger holds no state of its own to scribe - everything (bounds, latch, tracked
    /// filter, stockpile, count-all-on-map) lives per <see cref="CropRotationEntry"/>, scribed via
    /// <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/>.
    /// </summary>
    public override void ExposeData() => base.ExposeData();
}
