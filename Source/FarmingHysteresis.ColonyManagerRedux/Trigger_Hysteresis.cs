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
/// <c>Utilities.CountProducts</c>/<c>Utilities.CountProductsCoroutine</c> helpers
/// instead of this mod's own per-grower counting. By default (<see cref="TrackedFilterFollowsTargetPlant"/>)
/// the filter tracks whatever <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/> the job
/// pushes onto every grower it manages — matching this integration's original, simpler behavior
/// verbatim — until a player explicitly detaches it to track something else instead.
/// </summary>
internal sealed class Trigger_Hysteresis : Trigger
{
    private const float ProductIconSize = 24f;
    private const float ProductRowPadding = 5f;

    public int Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound;
    public int Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound;

    internal LatchMode LatchModeValue { get; private set; } = LatchMode.Unknown;
    internal int TrackedThingCount { get; private set; }

    public ThingFilter ParentFilter { get; private set; } = ThingFilter.CreateOnlyEverStorableThingFilter();

    private ThingFilter trackedThingFilter;

    public Trigger_Hysteresis(ManagerJob job)
        : base(job)
    {
        trackedThingFilter = new ThingFilter(TrackedThingFilter_SettingsChanged);
        trackedThingFilter.SetDisallowAll();
    }

    /// <summary>
    /// The filter this trigger actually counts against (see <see cref="RecomputeState"/>). Kept
    /// distinct from <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/> per Step 4 — see
    /// this class's own doc comment.
    /// </summary>
    public ThingFilter TrackedThingFilter => trackedThingFilter;

    private void TrackedThingFilter_SettingsChanged() => _cachedState.Invalidate();

    /// <summary>
    /// Whether <see cref="TrackedThingFilter"/> is kept in sync with
    /// <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/> (see
    /// <see cref="SyncTrackedFilterToTargetPlant"/>) rather than left to the player's own
    /// choice. Defaults to <see langword="true"/> so a freshly created job behaves exactly like
    /// this integration did before Step 4 — tracking whatever plant the job pushes onto its
    /// growers — until a player explicitly detaches it via <c>WindowTriggerHysteresisDetails</c>.
    /// </summary>
    public bool TrackedFilterFollowsTargetPlant { get; set; } = true;

    private Zone_Stockpile? stockpile;

    /// <summary>Restricts <see cref="TrackedThingFilter"/> counting to a single stockpile, mirroring <see cref="Trigger_Threshold.Stockpile"/>.</summary>
    public Zone_Stockpile? Stockpile
    {
        get => stockpile;
        set => stockpile = value;
    }

    public ref Zone_Stockpile? StockpileRef => ref stockpile;

    private string? _stockpileScribe;

    public bool CountAllOnMap;

    /// <summary>
    /// Pure "seed once" helper behind <see cref="SyncTrackedFilterToTargetPlant"/> - resets
    /// <paramref name="filter"/> to allow only <paramref name="def"/> (or nothing, if
    /// <see langword="null"/>). Split out as a static method taking a bare <see cref="ThingFilter"/>
    /// so it's unit-testable without a live job/trigger.
    /// </summary>
    internal static void SyncFilterToSingleDef(ThingFilter filter, ThingDef? def)
    {
        filter.SetDisallowAll();
        if (def != null)
        {
            filter.SetAllow(def, true);
        }
    }

    /// <summary>
    /// Called from <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/>'s setter whenever
    /// the job's target plant actually changes. No-ops unless
    /// <see cref="TrackedFilterFollowsTargetPlant"/> is on - the job doesn't need to know that
    /// detail itself.
    /// </summary>
    internal void SyncTrackedFilterToTargetPlant()
    {
        if (!TrackedFilterFollowsTargetPlant)
        {
            return;
        }

        SyncFilterToSingleDef(trackedThingFilter, HysteresisJob.TargetPlantDef?.plant.harvestedThingDef);
    }

    private readonly CachedValue<bool> _cachedState = new(false);

    private string? _lowerBoundBuffer;
    private string? _upperBoundBuffer;

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

    private bool RecomputeState()
    {
        // No more special-casing "no target plant chosen"/"plant has no harvested product" -
        // see this class's own doc comment (Step 4): TrackedThingFilter is independent of
        // TargetPlantDef now, and an empty/disallow-all filter (e.g. a brand new job) already
        // naturally counts 0 via CMR's own CountProducts. Whether the job actually has a plant
        // to sow is a separate concern, gated in ManagerJob_FarmingHysteresis.GatherJobDataCoroutine.
        var count = HysteresisJob.Manager.map.CountProducts(TrackedThingFilter, Stockpile, CountAllOnMap);
        TrackedThingCount = count;

        LatchModeValue = ComputeNextLatchMode(LatchModeValue, count, Lower, Upper);
        return LatchModeValue is LatchMode.BelowLowerBound or LatchMode.BetweenBoundsEnabled;
    }

    /// <inheritdoc/>
    public override bool State =>
        _cachedState.TryGetValue(out var value) ? value : _cachedState.Update(RecomputeState());

    /// <summary>
    /// <see cref="State"/> is otherwise only ever recomputed from
    /// <see cref="ManagerJob_FarmingHysteresis.GatherJobDataCoroutine"/>, which only runs on
    /// CMR's own ticking job schedule - it never runs at all while the game is paused, which is
    /// exactly when a player is likely to be poking at this job's scope/config in the UI. Reading
    /// <see cref="StatusTooltip"/> forces a fresh computation instead of showing whatever was
    /// last computed (possibly nothing, for a brand new job), so the displayed status can't lag
    /// behind an already-chosen target plant.
    /// </summary>
    public override string StatusTooltip
    {
        get
        {
            _ = State;
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
    /// A short label describing whatever <see cref="TrackedThingFilter"/> currently allows -
    /// the single allowed def's own label if there's exactly one (the common case, whether
    /// following the target plant or a player's own single-def choice), or a translated summary
    /// otherwise. Used for standalone display (the tracked-product row, the "configure tracked
    /// items" area) where naming how many kinds are tracked is useful context. For text that
    /// embeds a count (e.g. "fewer than 1000 {label} in storage"), use
    /// <see cref="TrackedCountNoun"/> instead - substituting this property there previously
    /// produced ungrammatical, ambiguous sentences like "fewer than 1000 2 kinds of items in
    /// storage" (unclear whether that meant per-kind or combined) once more than one def could be
    /// tracked - see this class's own doc comment, Step 4.
    /// </summary>
    private string TrackedLabel
    {
        get
        {
            var allowedDefs = TrackedThingFilter.AllowedThingDefs.ToList();
            return allowedDefs.Count switch
            {
                0 => "FarmingHysteresis.CMR.Trigger.TrackedSummary.None".Translate(),
                1 => allowedDefs[0].label,
                _ => "FarmingHysteresis.CMR.Trigger.TrackedSummary.Multiple".Translate(
                    allowedDefs.Count
                ),
            };
        }
    }

    /// <summary>
    /// A plain, pluralizable noun phrase for <see cref="TrackedThingFilter"/>'s current contents -
    /// the single allowed def's own label if there's exactly one, or the generic "items" otherwise
    /// (whether zero or several defs are allowed). Unlike <see cref="TrackedLabel"/>, this always
    /// reads correctly when substituted into a "{count} {noun} in storage" sentence, and always
    /// refers unambiguously to <see cref="TrackedThingCount"/>'s combined total rather than a
    /// per-kind amount - the exact set of tracked defs is what "Configure tracked items…" and the
    /// tracked-product row are for.
    /// </summary>
    private string TrackedCountNoun
    {
        get
        {
            var allowedDefs = TrackedThingFilter.AllowedThingDefs.ToList();
            return allowedDefs.Count == 1
                ? allowedDefs[0].label
                : "FarmingHysteresis.CMR.Trigger.TrackedCountNoun.Generic".Translate();
        }
    }

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

        if (
            Widgets.ButtonText(
                new Rect(cur.x, cur.y, width, entryHeight),
                "FarmingHysteresis.CMR.Trigger.ConfigureTracked".Translate()
            )
        )
        {
            Find.WindowStack.Add(
                new WindowTriggerHysteresisDetails(this) { closeOnClickedOutside = true, draggable = true }
            );
        }
        cur.y += entryHeight;

        DrawBoundEntry(
            ref cur,
            width,
            entryHeight,
            "FarmingHysteresis.CMR.Trigger.Lower".Translate(),
            ref Lower,
            ref _lowerBoundBuffer,
            "FarmingHysteresis.LowerBound".Translate(
                targetPlantDef.label,
                Lower,
                TrackedCountNoun,
                FarmingHysteresisMod.Settings.HysteresisMode.AsString()
            )
        );

        DrawBoundEntry(
            ref cur,
            width,
            entryHeight,
            "FarmingHysteresis.CMR.Trigger.Upper".Translate(),
            ref Upper,
            ref _upperBoundBuffer,
            "FarmingHysteresis.UpperBound".Translate(
                targetPlantDef.label,
                Upper,
                TrackedCountNoun,
                FarmingHysteresisMod.Settings.HysteresisMode.AsString()
            )
        );

        if (Lower < 0)
        {
            Lower = 0;
        }
        if (Upper < Lower)
        {
            Upper = Lower;
        }

        // Force a fresh read (see StatusTooltip's own doc comment) so the amount/state text
        // below can't lag behind bounds the player just edited above.
        _ = State;

        DrawWrappedLabel(
            ref cur,
            width,
            entryHeight,
            "FarmingHysteresis.InStorage".Translate(TrackedCountNoun, TrackedThingCount)
        );

        DrawWrappedLabel(
            ref cur,
            width,
            entryHeight,
            "FarmingHysteresis.LatchModeDesc".Translate(
                ("FarmingHysteresis.LatchModeDesc." + LatchModeValue).Translate(
                    FarmingHysteresisMod.Settings.HysteresisMode.AsString()
                )
            )
        );
    }

    /// <summary>
    /// The tracked-product row: <see cref="DrawProductRow"/>'s icon+label when
    /// <see cref="TrackedThingFilter"/> allows exactly one def (the common case - following the
    /// target plant, or a player's own single-def choice), or a plain summary label otherwise
    /// (see <see cref="TrackedLabel"/>) - the filter can no longer be assumed to always resolve
    /// to a single def once it's independent of <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/>
    /// (Step 4).
    /// </summary>
    private float DrawTrackedProductRow(Vector2 pos, float width)
    {
        var allowedDefs = TrackedThingFilter.AllowedThingDefs.ToList();
        if (allowedDefs.Count == 1)
        {
            return DrawProductRow(allowedDefs[0], pos, width);
        }

        var rowHeight = ProductIconSize + (2 * ProductRowPadding);
        var rect = new Rect(pos.x, pos.y, width, rowHeight);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect, TrackedLabel);
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
    /// A labeled bound row using vanilla's own <see cref="Widgets.IntEntry"/> stepper (the same
    /// "-10 / -1 / value / +1 / +10" control RimWorld's own numeric editors use, e.g.
    /// <c>Listing_Standard.IntEntry</c>) instead of a bare <see cref="Widgets.TextFieldNumeric"/>,
    /// plus a wrapped description line underneath - matching the legacy per-grower
    /// <c>ITab_Hysteresis</c>'s bounds editor instead of the plain half-width text fields this
    /// used to draw.
    /// </summary>
    private static void DrawBoundEntry(
        ref Vector2 cur,
        float width,
        float entryHeight,
        string label,
        ref int value,
        ref string? buffer,
        string description
    )
    {
        Widgets.Label(new Rect(cur.x, cur.y, width, entryHeight), label);
        cur.y += entryHeight;

        Widgets.IntEntry(new Rect(cur.x, cur.y, width, entryHeight), ref value, ref buffer);
        cur.y += entryHeight;

        DrawWrappedLabel(ref cur, width, entryHeight, description);
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
    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(
            ref Lower,
            "lower",
            FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound
        );
        Scribe_Values.Look(
            ref Upper,
            "upper",
            FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound
        );
        var latchMode = LatchModeValue;
        Scribe_Values.Look(ref latchMode, "latchMode", LatchMode.Unknown);
        LatchModeValue = latchMode;

        var followsTargetPlant = TrackedFilterFollowsTargetPlant;
        Scribe_Values.Look(ref followsTargetPlant, "trackedFilterFollowsTargetPlant", true);
        TrackedFilterFollowsTargetPlant = followsTargetPlant;

        Scribe_Deep.Look(
            ref trackedThingFilter,
            "trackedThingFilter",
            (object)TrackedThingFilter_SettingsChanged
        );
        Scribe_Values.Look(ref CountAllOnMap, "countAllOnMap");

        // Stockpile isn't referenceable - scribe by label, same as Trigger_Threshold.ExposeData.
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            _stockpileScribe = stockpile?.ToString() ?? "null";
        }

        Scribe_Values.Look(ref _stockpileScribe, "stockpile", "null");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            stockpile =
                Job.Manager.map.zoneManager.AllZones.FirstOrDefault(z =>
                    z is Zone_Stockpile && z.label == _stockpileScribe
                ) as Zone_Stockpile;
        }
    }
}
