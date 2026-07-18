using ColonyManagerRedux;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// This job's own upper/lower bound pair and latch state — the CMR-side replacement for
/// <see cref="FarmingHysteresisData"/>'s per-grower bookkeeping (see
/// <c>Docs/CMRIntegrationRework.md</c>, "Why <c>Trigger_Threshold</c> doesn't fit"). Unlike the
/// default engine, where every grower tracks its own harvested product independently, this
/// trigger tracks the stock of the job's own <see cref="ManagerJob_FarmingHysteresis.TargetPlantDef"/>
/// directly — the job pushes that one plant onto every grower it manages (see
/// <c>ManagerJob_FarmingHysteresis.ExecuteJobDataCoroutine</c>), so there's exactly one def to
/// track, not a guess at which grower's current selection to follow.
/// </summary>
internal sealed class Trigger_Hysteresis(ManagerJob job) : Trigger(job)
{
    private const float ProductIconSize = 24f;
    private const float ProductRowPadding = 5f;

    public int Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound;
    public int Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound;

    internal LatchMode LatchModeValue { get; private set; } = LatchMode.Unknown;
    internal int TrackedThingCount { get; private set; }

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
    internal static LatchMode ComputeNextLatchMode(LatchMode current, int count, int lower, int upper) =>
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
        var targetPlantDef = HysteresisJob.TargetPlantDef;
        if (targetPlantDef == null)
        {
            // No target plant chosen yet - leave latch memory alone (mirrors
            // FarmingHysteresisData.DisableDueToMissingHarvestedThingDef, which stops
            // monitoring without forcing a state) rather than guessing.
            TrackedThingCount = 0;
            return false;
        }

        var harvestedThingDef = targetPlantDef.plant.harvestedThingDef;
        if (harvestedThingDef == null)
        {
            TrackedThingCount = 0;
            return false;
        }

        var count = HysteresisJob.Manager.map.CountOfHarvestedThingDef(harvestedThingDef);
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
                    targetPlantDef.plant.harvestedThingDef?.label ?? targetPlantDef.label,
                    Lower,
                    Upper
                );
        }
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
        var harvestedThingDef = targetPlantDef?.plant.harvestedThingDef;

        if (targetPlantDef == null || harvestedThingDef == null)
        {
            DrawWrappedLabel(ref cur, width, entryHeight, StatusTooltip);
            return;
        }

        cur.y += DrawProductRow(harvestedThingDef, cur, width);

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
                harvestedThingDef.label,
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
                harvestedThingDef.label,
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
            "FarmingHysteresis.InStorage".Translate(harvestedThingDef.label, TrackedThingCount)
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

        Scribe_Values.Look(ref Lower, "lower", FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound);
        Scribe_Values.Look(ref Upper, "upper", FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound);
        var latchMode = LatchModeValue;
        Scribe_Values.Look(ref latchMode, "latchMode", LatchMode.Unknown);
        LatchModeValue = latchMode;
    }
}
