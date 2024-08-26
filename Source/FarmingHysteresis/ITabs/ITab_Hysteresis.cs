using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ITabs;

class ITab_Hysteresis : ITab
{
    private const float ProductIconSize = 24f;
    private const float ProductRowPadding = 5f;

    private string? _lowerBoundBuffer;
    private string? _upperBoundBuffer;
    private int _lowerBound;
    private int _upperBound;
    private bool _useGlobalValues;

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
        IPlantToGrowSettable plantToGrowSettable = (IPlantToGrowSettable)SelObject;
        var (harvestedThingDef, _) = plantToGrowSettable.PlantHarvestInfo();

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
            _useGlobalValues = data.useGlobalValues;
        }
    }

    protected override void FillTab()
    {
        var data = GetFarmingHysteresisData();

        IPlantToGrowSettable plantToGrowSettable = (IPlantToGrowSettable)SelObject;
        var (harvestedThingDef, harvestedThingCount) = plantToGrowSettable.PlantHarvestInfo();
        if (data == null || harvestedThingDef == null)
        {
            return;
        }

        Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

        Listing_Standard listingStandard = new()
        {
            maxOneColumn = true
        };

        listingStandard.Begin(rect);

        var productLabelRect = listingStandard.Label("FarmingHysteresis.ProductLabel".Translate());

        DrawProductRow(harvestedThingDef, productLabelRect.yMax);
        listingStandard.Gap(ProductIconSize + 3 * ProductRowPadding);
        listingStandard.GapLine(ProductRowPadding);
        listingStandard.Gap(5f);

        listingStandard.CheckboxLabeled("FarmingHysteresis.UseGlobalBoundsLabel".Translate(), ref _useGlobalValues);
        if (data.useGlobalValues != _useGlobalValues)
        {
            data.useGlobalValues = _useGlobalValues;
            _lowerBound = data.LowerBound;
            _upperBound = data.UpperBound;
            _lowerBoundBuffer = null;
            _upperBoundBuffer = null;
        }

        var plant = plantToGrowSettable.GetPlantDefToGrow();

        listingStandard.Label("FarmingHysteresis.LowerBoundLabel".Translate());
        listingStandard.IntEntry(ref _lowerBound, ref _lowerBoundBuffer);
        listingStandard.Label("FarmingHysteresis.LowerBound".Translate(plant.label, data.LowerBound, harvestedThingDef.label, HysteresisModeString));

        if (_lowerBound != data.LowerBound)
        {
            data.LowerBound = _lowerBound;
        }

        listingStandard.Label("FarmingHysteresis.UpperBoundLabel".Translate());
        listingStandard.IntEntry(ref _upperBound, ref _upperBoundBuffer);
        listingStandard.Label("FarmingHysteresis.UpperBound".Translate(plant.label, data.UpperBound, harvestedThingDef.label, HysteresisModeString));

        if (_upperBound != data.UpperBound)
        {
            data.UpperBound = _upperBound;
        }

        listingStandard.GapLine();

        listingStandard.Label("FarmingHysteresis.InStorage".Translate(harvestedThingDef.label, harvestedThingCount));
        listingStandard.Label("FarmingHysteresis.LatchModeDesc".Translate(("FarmingHysteresis.LatchModeDesc." + data.latchMode.ToString()).Translate(FarmingHysteresisMod.Settings.HysteresisMode.AsString())));

        listingStandard.End();

        size = new Vector2(440f, listingStandard.CurHeight + 24f);

        void DrawProductRow(ThingDef harvestedThingDef, float rowY)
        {
            Rect rowRect = new(0f, rowY, size.x - 4 * ProductRowPadding, ProductIconSize + 2 * ProductRowPadding);
            Rect harvestLabelRect = new(ProductIconSize + 2 * ProductRowPadding, rowY, rowRect.width - ProductIconSize + 2 * ProductRowPadding, rowRect.height);
            Rect harvestIconRect = new(5f, rowY + 5f, ProductIconSize, ProductIconSize);

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
                TipSignal tip = new(harvestedThingDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + harvestedThingDef.description);
                TooltipHandler.TipRegion(rowRect, tip);
            }

            if (Widgets.ButtonInvisible(rowRect, doMouseoverSound: false))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(harvestedThingDef));
            }
        }
    }

    public override bool IsVisible
    {
        get
        {
            return !Hidden;
        }
    }

#if v1_3
    public bool Hidden
#else
    public override bool Hidden
#endif
    {
        get
        {
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
    private static string _cachedHysteresisModeString = string.Empty;

    static string HysteresisModeString
    {
        get
        {
            if (_cachedHysteresisMode != FarmingHysteresisMod.Settings.HysteresisMode)
            {
                _cachedHysteresisMode = FarmingHysteresisMod.Settings.HysteresisMode;
                _cachedHysteresisModeString = ((HysteresisMode)_cachedHysteresisMode).AsString();
            }

            return _cachedHysteresisModeString;
        }
    }
}
