using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ITabs;

internal class ITab_Hysteresis : ITab
{
    private const float ProductIconSize = 24f;
    private const float ProductRowPadding = 5f;

    private string? _lowerBoundBuffer;
    private string? _upperBoundBuffer;
    private int _lowerBound;
    private int _upperBound;
    private BoundsSource _boundsSource;

    public ITab_Hysteresis()
    {
        labelKey = "FarmingHysteresis.TabHysteresis";
    }

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshFields();
    }

    public override void TabUpdate()
    {
        base.TabUpdate();
        RefreshFields();
    }

    private void RefreshFields()
    {
        var data = GetFarmingHysteresisData();
        var plantToGrowSettable = (IPlantToGrowSettable)SelObject;
        var harvestedThingDef = plantToGrowSettable.PlantHarvestDef();

        if (data != null && harvestedThingDef != null)
        {
            if (_lowerBound != data.LowerBound)
            {
                _lowerBound = data.LowerBound;
                _lowerBoundBuffer = null;
            }

            if (_upperBound != data.UpperBound)
            {
                _upperBound = data.UpperBound;
                _upperBoundBuffer = null;
            }
            _boundsSource = data.boundsSource;
        }
    }

    protected override void FillTab()
    {
        var data = GetFarmingHysteresisData();

        var plantToGrowSettable = (IPlantToGrowSettable)SelObject;
        var (harvestedThingDef, harvestedThingCount) = plantToGrowSettable.PlantHarvestInfo();
        if (data == null || harvestedThingDef == null)
        {
            return;
        }

        var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

        Listing_Standard listingStandard = new() { maxOneColumn = true };

        listingStandard.Begin(rect);

        var productLabelRect = listingStandard.Label("FarmingHysteresis.ProductLabel".Translate());

        DrawProductRow(harvestedThingDef, productLabelRect.yMax);
        listingStandard.Gap(ProductIconSize + (3 * ProductRowPadding));
        listingStandard.GapLine(ProductRowPadding);
        listingStandard.Gap(5f);

        DrawBoundsSourceRow(listingStandard, data, plantToGrowSettable, harvestedThingDef);

        var plant = plantToGrowSettable.GetPlantDefToGrow();

        DrawBoundRow(
            listingStandard,
            ref _lowerBound,
            ref _lowerBoundBuffer,
            data.LowerBound,
            value => data.LowerBound = value,
            "FarmingHysteresis.LowerBoundLabel",
            "FarmingHysteresis.LowerBound",
            plant,
            harvestedThingDef
        );

        DrawBoundRow(
            listingStandard,
            ref _upperBound,
            ref _upperBoundBuffer,
            data.UpperBound,
            value => data.UpperBound = value,
            "FarmingHysteresis.UpperBoundLabel",
            "FarmingHysteresis.UpperBound",
            plant,
            harvestedThingDef
        );

        listingStandard.GapLine();

        _ = listingStandard.Label(
            "FarmingHysteresis.InStorage".Translate(harvestedThingDef.label, harvestedThingCount)
        );
        _ = listingStandard.Label(
            "FarmingHysteresis.LatchModeDesc".Translate(
                ("FarmingHysteresis.LatchModeDesc." + data.latchMode.ToString()).Translate(
                    FarmingHysteresisMod.Settings.HysteresisMode.AsString()
                )
            )
        );

        listingStandard.End();

        size = new Vector2(440f, listingStandard.CurHeight + 24f);
    }

    private void DrawBoundsSourceRow(
        Listing_Standard listingStandard,
        FarmingHysteresisData data,
        IPlantToGrowSettable plantToGrowSettable,
        ThingDef harvestedThingDef
    )
    {
        if (
            listingStandard.ButtonTextLabeledCompat(
                "FarmingHysteresis.BoundsSourceLabel".Translate(),
                BoundsSourceUi.Label(_boundsSource)
            )
        )
        {
            BoundsSourceUi.OpenFloatMenu(
                data,
                plantToGrowSettable,
                harvestedThingDef,
                onSwitched: () =>
                {
                    _boundsSource = data.boundsSource;
                    _lowerBound = data.LowerBound;
                    _upperBound = data.UpperBound;
                    _lowerBoundBuffer = null;
                    _upperBoundBuffer = null;
                }
            );
        }
    }

    private static void DrawBoundRow(
        Listing_Standard listingStandard,
        ref int bound,
        ref string? buffer,
        int dataBound,
        Action<int> setDataBound,
        string labelTranslationKey,
        string descTranslationKey,
        ThingDef plant,
        ThingDef harvestedThingDef
    )
    {
        _ = listingStandard.Label(labelTranslationKey.Translate());
        listingStandard.IntEntry(ref bound, ref buffer);
        _ = listingStandard.Label(
            descTranslationKey.Translate(plant.label, dataBound, harvestedThingDef.label, HysteresisModeString)
        );

        if (bound != dataBound)
        {
            setDataBound(bound);
        }
    }

    private void DrawProductRow(ThingDef harvestedThingDef, float rowY)
    {
        Rect rowRect = new(
            0f,
            rowY,
            size.x - (4 * ProductRowPadding),
            ProductIconSize + (2 * ProductRowPadding)
        );
        Rect harvestLabelRect = new(
            ProductIconSize + (2 * ProductRowPadding),
            rowY,
            rowRect.width - (ProductIconSize + (2 * ProductRowPadding)),
            rowRect.height
        );
        Rect harvestIconRect = new(5f, rowY + 5f, ProductIconSize, ProductIconSize);

        IlyvionDebugViewSettings.DrawIfUIHelpers(() =>
        {
            Widgets.DrawRectFast(harvestLabelRect, ColorLibrary.PaleGreen.ToTransparent(.5f));
            Widgets.DrawRectFast(harvestIconRect, ColorLibrary.PaleBlue.ToTransparent(.5f));
        });

        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        Widgets.DrawHighlightIfMouseover(rowRect);
        GUI.color = Color.white;

        GUI.DrawTexture(harvestIconRect, harvestedThingDef.uiIcon);

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(harvestLabelRect, harvestedThingDef.LabelCap);
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
    }

    public override bool IsVisible => !Hidden;

#if v1_3
    public bool Hidden
#else
    public override bool Hidden
#endif
    {
        get
        {
            if (!FarmingHysteresisMod.HysteresisController.ShowGrowerUi)
            {
                return true;
            }

            var data = GetFarmingHysteresisData();
            return !(data?.Enabled ?? false);
        }
    }

    private FarmingHysteresisData? GetFarmingHysteresisData()
    {
        FarmingHysteresisData? data = null;
        if (SelObject is IPlantToGrowSettable plantGrower)
        {
            data = plantGrower.GetFarmingHysteresisData();
        }
        return data;
    }

    private static HysteresisMode? _cachedHysteresisMode;

    private static string HysteresisModeString
    {
        get
        {
            if (_cachedHysteresisMode != FarmingHysteresisMod.Settings.HysteresisMode)
            {
                _cachedHysteresisMode = FarmingHysteresisMod.Settings.HysteresisMode;
                field = ((HysteresisMode)_cachedHysteresisMode).AsString();
            }

            return field;
        }
    } = string.Empty;
}
