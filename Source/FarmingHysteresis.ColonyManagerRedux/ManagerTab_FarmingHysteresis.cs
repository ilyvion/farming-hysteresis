using ColonyManagerRedux;
using FarmingHysteresis.Defs;
using ilyvion.Laboratory.Extensions;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

internal sealed class ManagerTab_FarmingHysteresis(Manager manager)
    : ManagerTab<ManagerJob_FarmingHysteresis, ManagerSettings_FarmingHysteresis>(manager)
{
    private const float TargetPlantIconSize = 24f;
    private const float TargetPlantRowPadding = 5f;

    /// <summary>
    /// Whenever the migration gate is still suppressing takeover for this save, show the migrate
    /// notice even with no job selected - there's nothing useful to configure in the meantime
    /// (see <see cref="DoMainContent"/>), unlike the base class's default of a blank panel.
    /// </summary>
    protected override bool DoMainContentWhenNothingSelected => IsMigrationPending;

    /// <summary>
    /// Flags a job as dormant, in both this tab's own job list and CMR's overview tab (both call
    /// this - see <c>ManagerTab.DrawLocalListEntry</c>/<c>ManagerTab_Overview</c>'s row drawing),
    /// whenever it's committed to <see cref="JobTracker"/> but CMR isn't currently the active
    /// hysteresis controller (see <see cref="ManagerJob_FarmingHysteresis.IsManaged"/>) - either
    /// because "take over Farming Hysteresis control" is off in mod settings, or a per-save
    /// <see cref="CmrMigrationGate"/> is still suppressing it. Without this, such a job looked
    /// identical to a normally-running one even though it's no longer touching any growers.
    /// </summary>
    public override string GetSubLabel(ManagerJob job) =>
        job is ManagerJob_FarmingHysteresis { IsCommittedToTracker: true, IsManaged: false }
            ? "FarmingHysteresis.CMR.JobDormantTakeoverOff".Translate() + " - " + base.GetSubLabel(job)
            : base.GetSubLabel(job);

    private static bool IsMigrationPending =>
        CmrMigrationGameComponent.CurrentStatus
            is CmrMigrationGateStatus.AwaitingChoice
                or CmrMigrationGateStatus.Declined;

    protected override void DoMainContent(Rect rect)
    {
        if (IsMigrationPending)
        {
            DrawMigrationPendingNotice(rect);
            return;
        }

        Widgets.DrawMenuSection(rect);

        var columnRect = new Rect(
            rect.xMin,
            rect.yMin,
            rect.width,
            rect.height - Margin - ButtonSize.y
        );
        var buttonRect = new Rect(
            rect.xMax - ButtonSize.x,
            rect.yMax - ButtonSize.y,
            ButtonSize.x - Margin,
            ButtonSize.y - Margin
        );

        Widgets_Section.BeginSectionColumn(
            columnRect,
            "FarmingHysteresis.Job",
            out var position,
            out var width
        );
        // Each rotation entry draws its own tracked-product icon/label and current storage count
        // (see DrawRotationEntryTrackedItems) rather than there being a separate, job-wide
        // "Trigger" section - keeping bounds, tracked items, and status together per-entry is
        // what fixes the disconnect between the crop rotation list and "what determines when a
        // crop finishes" (see Docs/CMRIntegrationRework.md's Step 5 follow-up).
        DrawSection(
            "FarmingHysteresis.Job",
            "CropRotation",
            ref position,
            width,
            DrawCropRotation,
            "FarmingHysteresis.CMR.CropRotation".Translate()
        );
        // Grower scope's zone list can grow arbitrarily long (one row per growing zone in the
        // game), so it's placed last to avoid pushing the shorter, fixed-height sections below it.
        DrawSection(
            "FarmingHysteresis.Job",
            "GrowerScope",
            ref position,
            width,
            DrawGrowerScope,
            "FarmingHysteresis.CMR.GrowerScope".Translate()
        );
        Widgets_Section.EndSectionColumn("FarmingHysteresis.Job", position);

        DrawManageButton(buttonRect);
    }

    /// <summary>
    /// The "Manage!"/"Delete" toggle every other stock CMR tab shows for its selected job - without
    /// this, a configured job never actually gets added to <see cref="JobTracker"/> and so never
    /// runs (see <see cref="ManagerJob_FarmingHysteresis.IsManaged"/>), leaving no way for the
    /// player to actually put a job into effect from this tab. Deliberately keyed off
    /// <see cref="ManagerJob_FarmingHysteresis.IsCommittedToTracker"/> rather than
    /// <see cref="ManagerJob_FarmingHysteresis.IsManaged"/> - the latter also folds in whether CMR
    /// is currently the active controller, so a job that's already committed but temporarily
    /// dormant would otherwise show "Manage!" again and get re-added to the tracker as a duplicate.
    /// </summary>
    private void DrawManageButton(Rect buttonRect)
    {
        var job = SelectedJob!;
        if (!job.IsCommittedToTracker)
        {
            if (Widgets.ButtonText(buttonRect, "ColonyManagerRedux.Common.Manage".Translate()))
            {
                job.IsManaged = true;
                Manager.JobTracker.Add(job);
                Refresh();
            }
        }
        else
        {
            if (Widgets.ButtonText(buttonRect, "ColonyManagerRedux.Common.Delete".Translate()))
            {
                Manager.JobTracker.Delete(job);
                Selected = MakeNewJob();
                Refresh();
            }
        }
    }

    private const float ReorderButtonSize = 22f;

    /// <summary>
    /// The "Crop rotation" section (see <c>Docs/CMRIntegrationRework.md</c>, Step 5 - resolves
    /// #6): a switch-mode toggle, then <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/>
    /// as an ordered, editable list - each entry's own bounds decide when the job moves on to the
    /// next one, cycling. Replaces the old single "Target plant" section; a job with exactly one
    /// entry behaves exactly like this integration's original one-crop-per-job design.
    /// </summary>
    private static float DrawCropRotation(ManagerJob_FarmingHysteresis job, Vector2 pos, float width)
    {
        var start = pos;

        DrawRotationModeSelector(job, ref pos, width);
        DrawSwitchModeSelector(job, ref pos, width);

        var validTargetPlants = job.ValidTargetPlants.ToList();
        if (validTargetPlants.Count == 0 && job.RotationEntries.Count == 0)
        {
            var message = "FarmingHysteresis.CMR.TargetPlant.NoneAvailable".Translate();
            var emptyHeight = Mathf.Max(ListEntryHeight, Text.CalcHeight(message, width));
            Widgets.Label(new Rect(pos.x, pos.y, width, emptyHeight), message);
            pos.y += emptyHeight;
            return pos.y - start.y;
        }

        pos.y += DrawRotationEntries(job, validTargetPlants, pos, width);
        pos.y += DrawAddCropButton(job, validTargetPlants, pos, width);

        return pos.y - start.y;
    }

    /// <summary>
    /// Per-job choice between the two rotation semantics (see <see cref="RotationMode"/>) - same
    /// toggle-pair shape as <see cref="DrawSwitchModeSelector"/>/<see cref="DrawAssignmentModeSelector"/>.
    /// </summary>
    private static void DrawRotationModeSelector(
        ManagerJob_FarmingHysteresis job,
        ref Vector2 pos,
        float width
    )
    {
        var modes = (RotationMode[])Enum.GetValues(typeof(RotationMode));
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                $"FarmingHysteresis.CMR.RotationMode.{mode}".Translate(),
                $"FarmingHysteresis.CMR.RotationMode.{mode}.Tip".Translate(),
                job.Mode == mode,
                () => job.Mode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }

        pos.y += ListEntryHeight;
    }

    private static void DrawSwitchModeSelector(
        ManagerJob_FarmingHysteresis job,
        ref Vector2 pos,
        float width
    )
    {
        var modes = (RotationSwitchMode[])Enum.GetValues(typeof(RotationSwitchMode));
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                $"FarmingHysteresis.CMR.RotationSwitchMode.{mode}".Translate(),
                $"FarmingHysteresis.CMR.RotationSwitchMode.{mode}.Tip".Translate(),
                job.SwitchMode == mode,
                () => job.SwitchMode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }

        pos.y += ListEntryHeight;
    }

    /// <summary>
    /// Every rotation entry, in order, modeled on CMR's own
    /// <c>ManagerTab_Mining.DrawTaskPriorityOrder</c>: a per-row up/down reorder pair
    /// (<c>Utilities.DrawReorderButton</c>, no-ops at either boundary) and a delete button,
    /// with the plant picker (styled like the old single-crop picker) and that entry's own
    /// Lower/Upper bounds underneath. Move/removal is deferred and applied once after the loop,
    /// same as <c>DrawTaskPriorityOrder</c>, to avoid mutating <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/>
    /// mid-iteration.
    /// </summary>
    private static float DrawRotationEntries(
        ManagerJob_FarmingHysteresis job,
        List<ThingDef> validTargetPlants,
        Vector2 pos,
        float width
    )
    {
        var start = pos;
        var entries = job.RotationEntries;

        (int index, int delta)? move = null;
        var removeIndex = -1;

        for (var i = 0; i < entries.Count; i++)
        {
            if (Widgets_Section.CanCull(pos.y, RotationEntryHeight))
            {
                pos.y += RotationEntryHeight;
                continue;
            }

            var entry = entries[i];
            var top = i == 0;
            var bottom = i == entries.Count - 1;

            var rowRect = new Rect(pos.x, pos.y, width, TargetPlantIconSize + (2 * TargetPlantRowPadding));
            var buttonsWidth = (3 * ReorderButtonSize) + (2 * Margin);
            var pickerRect = new Rect(rowRect.x, rowRect.y, rowRect.width - buttonsWidth - Margin, rowRect.height);

            DrawRotationEntryPickerRow(job, entry, validTargetPlants, pickerRect, i, isActive: entry.Id == job.ActiveEntryId);

            var buttonY = rowRect.y + ((rowRect.height - ReorderButtonSize) / 2);
            var upRect = new Rect(pickerRect.xMax + Margin, buttonY, ReorderButtonSize, ReorderButtonSize);
            var downRect = new Rect(upRect.xMax, buttonY, ReorderButtonSize, ReorderButtonSize);
            var deleteRect = new Rect(downRect.xMax, buttonY, ReorderButtonSize, ReorderButtonSize);

            var capturedIndex = i;
            _ = Utilities.DrawReorderButton(
                upRect,
                Resources.ArrowUp,
                "FarmingHysteresis.CMR.CropRotation.MoveUp".Translate(),
                top,
                () => move = (capturedIndex, -1)
            );
            _ = Utilities.DrawReorderButton(
                downRect,
                Resources.ArrowDown,
                "FarmingHysteresis.CMR.CropRotation.MoveDown".Translate(),
                bottom,
                () => move = (capturedIndex, 1)
            );
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
            {
                removeIndex = capturedIndex;
            }
            TooltipHandler.TipRegion(deleteRect, "FarmingHysteresis.CMR.CropRotation.Remove".Translate());

            pos.y = rowRect.yMax;
            pos.y += DrawRotationEntryBounds(job, entry, pos, width);
            pos.y += DrawRotationEntryTrackedItems(job, entry, pos, width);
        }

        if (move is { } m)
        {
            job.MoveRotationEntry(m.index, m.delta);
        }
        else if (removeIndex >= 0)
        {
            job.RemoveRotationEntry(removeIndex);
        }

        return pos.y - start.y;
    }

    private static float RotationEntryHeight =>
        // Picker row + tracked-product row share the same icon+label shape/size.
        (2 * (TargetPlantIconSize + (2 * TargetPlantRowPadding)))
        // Bounds (2 rows of label+field each) + storage amount + latch state + configure button.
        // The latch state line can wrap onto a second line depending on translated text/width -
        // this only affects DrawRotationEntries' cull-skip advancement (Widgets_Section.CanCull),
        // same approximation level already used elsewhere in this file for label rows.
        + (7 * ListEntryHeight)
        + Margin;

    /// <summary>
    /// The plant picker itself, styled to match <c>Trigger_Hysteresis.DrawProductRow</c> (icon +
    /// label over a hover highlight, opened via an invisible button) instead of a plain
    /// <c>Widgets.ButtonText</c> - a generic-looking button doesn't read as well here as
    /// the growers' own "Plant: X" gizmo does. The FloatMenu options mirror vanilla's own
    /// <c>Command_SetPlantToGrow.ProcessInput</c> construction (icon via the
    /// <c>shownItemForIcon</c> overload, trailing info-card button) rather than plain text
    /// entries, so it looks and behaves the same as the grower's own plant picker. Options
    /// exclude plants already used by another rotation entry, to keep each entry's tracked crop
    /// unambiguous. <paramref name="isActive"/> highlights whichever entry is currently the one
    /// being pushed onto managed growers - without this, nothing in the list distinguished it from
    /// the others, which was the root of the "which crop's bounds/tracked items are actually in
    /// effect right now" confusion this follow-up fixes.
    /// </summary>
    private static void DrawRotationEntryPickerRow(
        ManagerJob_FarmingHysteresis job,
        CropRotationEntry entry,
        List<ThingDef> validTargetPlants,
        Rect rowRect,
        int entryIndex,
        bool isActive
    )
    {
        var plantDef = entry.PlantDef;

        if (isActive)
        {
            Widgets.DrawHighlight(rowRect);
        }
        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        Widgets.DrawHighlightIfMouseover(rowRect);
        GUI.color = Color.white;

        if (plantDef != null)
        {
            var iconRect = new Rect(
                rowRect.x + TargetPlantRowPadding,
                rowRect.y + TargetPlantRowPadding,
                TargetPlantIconSize,
                TargetPlantIconSize
            );
            Widgets.DefIcon(iconRect, plantDef);

            var labelRect = new Rect(
                iconRect.xMax + TargetPlantRowPadding,
                rowRect.y,
                rowRect.width - iconRect.width - (3 * TargetPlantRowPadding),
                rowRect.height
            );
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                labelRect,
                (
                    isActive
                        ? "FarmingHysteresis.CMR.CropRotation.ActiveEntryLabel"
                        : "FarmingHysteresis.CMR.CropRotation.EntryLabel"
                ).Translate(entryIndex + 1, plantDef.LabelCap)
            );
            Text.Anchor = TextAnchor.UpperLeft;
        }
        else
        {
            var labelRect = new Rect(
                rowRect.x + TargetPlantRowPadding,
                rowRect.y,
                rowRect.width - (2 * TargetPlantRowPadding),
                rowRect.height
            );
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, "FarmingHysteresis.CMR.TargetPlant.None".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        if (Widgets.ButtonInvisible(rowRect, doMouseoverSound: false))
        {
            var options = validTargetPlants
                .Where(candidate => candidate == plantDef || !job.RotationEntries.Any(e => e.PlantDef == candidate))
                .Select(candidate => new FloatMenuOption(
                    candidate.LabelCap,
                    () => entry.PlantDef = candidate,
                    candidate,
                    null,
                    forceBasicStyle: false,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    29f,
                    rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + ((rect.height - 24f) / 2f), candidate)
                ))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    /// <summary>
    /// An entry's own Lower/Upper bound editor - the same <see cref="Widgets.IntEntry"/> stepper
    /// <c>Trigger_Hysteresis</c> used to draw once for the whole job (before Step 5 gave each
    /// rotation entry its own bounds), minus the long per-row description paragraph (impractical
    /// once a job can have several entries) - a hover tooltip carries the same explanation instead.
    /// </summary>
    private static float DrawRotationEntryBounds(
        ManagerJob_FarmingHysteresis job,
        CropRotationEntry entry,
        Vector2 pos,
        float width
    )
    {
        var start = pos;
        var tip = entry.PlantDef == null
            ? null
            : (TipSignal?)
                "FarmingHysteresis.CMR.CropRotation.BoundsTip".Translate(
                    entry.PlantDef.label,
                    FarmingHysteresisMod.Settings.HysteresisMode.AsString(),
                    job.RotationEntries.Count > 1
                        ? "FarmingHysteresis.CMR.CropRotation.BoundsTip.NextCrop".Translate()
                        : "FarmingHysteresis.CMR.CropRotation.BoundsTip.Stop".Translate()
                );

        Widgets.Label(new Rect(pos.x, pos.y, width, ListEntryHeight), "FarmingHysteresis.CMR.Trigger.Lower".Translate());
        pos.y += ListEntryHeight;
        var lowerRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        Widgets.IntEntry(lowerRect, ref entry.Lower, ref entry.LowerBuffer);
        if (tip != null)
        {
            TooltipHandler.TipRegion(lowerRect, tip.Value);
        }
        pos.y += ListEntryHeight;

        Widgets.Label(new Rect(pos.x, pos.y, width, ListEntryHeight), "FarmingHysteresis.CMR.Trigger.Upper".Translate());
        pos.y += ListEntryHeight;
        var upperRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        Widgets.IntEntry(upperRect, ref entry.Upper, ref entry.UpperBuffer);
        if (tip != null)
        {
            TooltipHandler.TipRegion(upperRect, tip.Value);
        }
        pos.y += ListEntryHeight;

        if (entry.Lower < 0)
        {
            entry.Lower = 0;
        }
        if (entry.Upper < entry.Lower)
        {
            entry.Upper = entry.Lower;
        }

        return pos.y - start.y;
    }

    /// <summary>
    /// An entry's own tracked items section - the same icon+label row (and, when the filter
    /// allows more than one def, plain summary label) this trigger used to show once for the
    /// whole job in a separate, disconnected section, plus a "Current amount of X in storage: N"
    /// line and the "Configure tracked items…" button that opens a
    /// <see cref="WindowTriggerHysteresisDetails"/> scoped to this entry alone. Moved here, and
    /// duplicated per entry, from the job-wide <c>Trigger_Hysteresis.DrawTriggerConfig</c> (see
    /// <c>Docs/CMRIntegrationRework.md</c>'s Step 5 follow-up) since what determines "this crop is
    /// done" often differs per crop - showing only the active entry's icon/count in a lone section
    /// at the bottom of the list gave no indication of what any of the *other* entries were
    /// tracking.
    /// </summary>
    private static float DrawRotationEntryTrackedItems(
        ManagerJob_FarmingHysteresis job,
        CropRotationEntry entry,
        Vector2 pos,
        float width
    )
    {
        var start = pos;

        pos.y += Trigger_Hysteresis.DrawTrackedProductRow(entry.TrackedThingFilter, pos, width);

        var count = job.Manager.map.CountProducts(entry.TrackedThingFilter, entry.Stockpile, entry.CountAllOnMap);
        var noun = Trigger_Hysteresis.DescribeTrackedCountNoun(entry.TrackedThingFilter);
        var storageRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        Widgets.Label(storageRect, "FarmingHysteresis.InStorage".Translate(noun, count));
        pos.y += ListEntryHeight;

        // This entry's own hysteresis state as of the last manager job cycle (see
        // CropRotationEntry.LatchModeValue) - without this, only the job's active entry ever
        // showed anything like the built-in Farming Hysteresis window's per-zone status line, and
        // there was no way to tell whether an inactive entry was BetweenBoundsEnabled or
        // BetweenBoundsDisabled (i.e. whether it'll start growing again the moment it becomes
        // active) just from its bounds and current count.
        var latchDescription = Trigger_Hysteresis.DescribeLatchMode(entry.LatchModeValue);
        var latchHeight = Mathf.Max(ListEntryHeight, Text.CalcHeight(latchDescription, width));
        Widgets.Label(new Rect(pos.x, pos.y, width, latchHeight), latchDescription);
        pos.y += latchHeight;

        var buttonRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        if (Widgets.ButtonText(buttonRect, "FarmingHysteresis.CMR.Trigger.ConfigureTracked".Translate()))
        {
            Find.WindowStack.Add(
                new WindowTriggerHysteresisDetails(entry, job.Manager) { closeOnClickedOutside = true, draggable = true }
            );
        }
        pos.y += ListEntryHeight;

        pos.y += Margin;

        return pos.y - start.y;
    }

    private static float DrawAddCropButton(
        ManagerJob_FarmingHysteresis job,
        List<ThingDef> validTargetPlants,
        Vector2 pos,
        float width
    )
    {
        var addable = validTargetPlants.Where(plantDef => !job.RotationEntries.Any(e => e.PlantDef == plantDef)).ToList();
        if (addable.Count == 0)
        {
            return 0f;
        }

        var buttonRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        if (Widgets.ButtonText(buttonRect, "FarmingHysteresis.CMR.CropRotation.AddCrop".Translate()))
        {
            var options = addable
                .Select(plantDef => new FloatMenuOption(
                    plantDef.LabelCap,
                    () => job.AddRotationEntry(plantDef),
                    plantDef,
                    null,
                    forceBasicStyle: false,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    29f,
                    rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + ((rect.height - 24f) / 2f), plantDef)
                ))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        return ListEntryHeight;
    }

    /// <summary>
    /// Shown in place of the usual job-editing panel while <see cref="CmrMigrationGameComponent"/>
    /// is still suppressing takeover for this save (dialog not yet answered, or answered "no") -
    /// configuring jobs wouldn't do anything useful yet since the old always-on engine, not CMR,
    /// is the one actually controlling growers. Doubles as the on-demand retry the player asked
    /// for after declining the initial one-time dialog.
    /// </summary>
    private static void DrawMigrationPendingNotice(Rect rect)
    {
        Widgets.DrawMenuSection(rect);

        var innerRect = rect.ContractedBy(Margin);
        var message = "FarmingHysteresis.CMR.Migration.PendingNotice".Translate();
        var messageHeight = Text.CalcHeight(message, innerRect.width);
        Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, messageHeight), message);

        var buttonRect = new Rect(
            innerRect.x,
            innerRect.y + messageHeight + Margin,
            ButtonSize.x,
            ButtonSize.y
        );
        if (Widgets.ButtonText(buttonRect, "FarmingHysteresis.CMR.Migration.MigrateNow".Translate()))
        {
            CmrMigrationGate.Migrate();
        }
    }

    private static void DrawAssignmentModeSelector(
        ManagerJob_FarmingHysteresis job,
        ref Vector2 pos,
        float width
    )
    {
        var modes = (GrowerAssignmentMode[])Enum.GetValues(typeof(GrowerAssignmentMode));
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                $"FarmingHysteresis.CMR.GrowerScope.{mode}".Translate(),
                $"FarmingHysteresis.CMR.GrowerScope.{mode}.Tip".Translate(),
                job.AssignmentMode == mode,
                () => job.AssignmentMode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }

        pos.y += ListEntryHeight;
    }

    private static float DrawGrowerScope(ManagerJob_FarmingHysteresis job, Vector2 pos, float width)
    {
        var start = pos;

        DrawAssignmentModeSelector(job, ref pos, width);

        switch (job.AssignmentMode)
        {
            case GrowerAssignmentMode.Area:
                global::ColonyManagerRedux.AreaAllowedGUI.DoAllowedAreaSelectors(
                    ref pos,
                    width,
                    ref job.GrowerArea,
                    5,
                    job.Manager
                );
                Utilities.DrawToggle(
                    ref pos,
                    width,
                    "ColonyManagerRedux.InvertArea".Translate(),
                    "ColonyManagerRedux.InvertArea.Tip".Translate(),
                    ref job.InvertGrowerArea
                );
                break;
            case GrowerAssignmentMode.Specific:
                pos.y += DrawSpecificGrowers(job, pos, width);
                break;
            case GrowerAssignmentMode.All:
            default:
                break;
        }

        return pos.y - start.y;
    }

    private static float DrawSpecificGrowers(
        ManagerJob_FarmingHysteresis job,
        Vector2 pos,
        float width
    )
    {
        var growers = FarmingHysteresisControlDef
            .AllControlledPlantGrowers(job.Manager.map)
            .ToList();

        if (growers.Count == 0)
        {
            var emptyRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            Widgets.Label(emptyRect, "FarmingHysteresis.CMR.GrowerScope.NoEligibleGrowers".Translate());
            return ListEntryHeight;
        }

        var start = pos;
        var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        foreach (var grower in growers)
        {
            if (Widgets_Section.CanCull(rowRect.y, rowRect.height))
            {
                rowRect.y += ListEntryHeight;
                continue;
            }

            var owningJob = ManagerJob_FarmingHysteresis.FindOwningJob(job.Manager, grower);
            var claimedByOtherJob = owningJob != null && owningJob != job;
            var label = ManagerJob_FarmingHysteresis.GrowerLabel(grower);

            if (claimedByOtherJob)
            {
                var wasEnabled = GUI.enabled;
                GUI.enabled = false;
                var allowed = IsSpecificallySelected(job, grower);
                Utilities.DrawToggle(
                    rowRect,
                    label,
                    "FarmingHysteresis.CMR.GrowerScope.ManagedByOtherJob".Translate(
                        owningJob!.TargetsLabel
                    ),
                    allowed,
                    () => { },
                    () => { }
                );
                GUI.enabled = wasEnabled;
            }
            else
            {
                var allowed = IsSpecificallySelected(job, grower);
                Utilities.DrawToggle(
                    rowRect,
                    label,
                    (TipSignal)label,
                    allowed,
                    () => SetSpecificallySelected(job, grower, true),
                    () => SetSpecificallySelected(job, grower, false)
                );
            }

            if (Mouse.IsOver(rowRect) && !Find.CameraDriver.IsPanning())
            {
                switch (grower)
                {
                    case Thing thing:
                        CameraJumper.TryJump(thing);
                        break;
                    case Zone zone when zone.Cells.Count > 0:
                        CameraJumper.TryJump(zone.Cells.First(), zone.Map);
                        break;
                    default:
                        break;
                }
            }

            rowRect.y += ListEntryHeight;
        }

        return rowRect.y - start.y;
    }

    private static bool IsSpecificallySelected(
        ManagerJob_FarmingHysteresis job,
        IPlantToGrowSettable grower
    ) =>
        grower switch
        {
            Zone zone => job.SpecificGrowingZones.Contains(zone),
            Building_PlantGrower building => job.SpecificPlantGrowerBuildings.Contains(building),
            _ => false,
        };

    private static void SetSpecificallySelected(
        ManagerJob_FarmingHysteresis job,
        IPlantToGrowSettable grower,
        bool selected
    )
    {
        switch (grower)
        {
            case Zone zone when selected:
                _ = job.SpecificGrowingZones.Add(zone);
                break;
            case Zone zone:
                _ = job.SpecificGrowingZones.Remove(zone);
                break;
            case Building_PlantGrower building when selected:
                _ = job.SpecificPlantGrowerBuildings.Add(building);
                break;
            case Building_PlantGrower building:
                _ = job.SpecificPlantGrowerBuildings.Remove(building);
                break;
            default:
                break;
        }
        job.Notify_TargetsChanged();
    }
}
