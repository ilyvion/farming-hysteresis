using RimWorld;
using Verse;
using UnityEngine;

namespace FarmingHysteresis.Patch
{
    internal class Dialog_HysteresisParameters : Window
    {

        private FarmingHysteresisData _data;

        private string? _lowerBoundBuffer;
        private string? _upperBoundBuffer;
        private int _lowerBound;
        private int _upperBound;
        private bool _useGlobalValues;

        public override Vector2 InitialSize => new Vector2(500f, 210f);

        public Dialog_HysteresisParameters(FarmingHysteresisData data)
        {
            forcePause = true;
            closeOnClickedOutside = true;

            _data = data;
            _lowerBound = data.LowerBound;
            _upperBound = data.UpperBound;
            _useGlobalValues = data.useGlobalValues;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("FarmingHysteresis.UseGlobalBoundsLabel".Translate(), ref _useGlobalValues);
            if (_data.useGlobalValues != _useGlobalValues)
            {
                _data.useGlobalValues = _useGlobalValues;
                _lowerBound = _data.LowerBound;
                _upperBound = _data.UpperBound;
                _lowerBoundBuffer = null;
                _upperBoundBuffer = null;
            }

            if (_lowerBound != _data.LowerBound)
            {
                _data.LowerBound = _lowerBound;
                if (_lowerBound != _data.LowerBound)
                {
                    _lowerBound = _data.LowerBound;
                    _lowerBoundBuffer = null;
                }
            }
            if (_upperBound != _data.UpperBound)
            {
                _data.UpperBound = _upperBound;
                if (_upperBound != _data.UpperBound)
                {
                    _upperBound = _data.UpperBound;
                    _upperBoundBuffer = null;
                }
            }

            listingStandard.Label("FarmingHysteresis.LowerBoundLabel".Translate());
            listingStandard.IntEntry(ref _lowerBound, ref _lowerBoundBuffer);

            listingStandard.Label("FarmingHysteresis.UpperBoundLabel".Translate());
            listingStandard.IntEntry(ref _upperBound, ref _upperBoundBuffer);

            listingStandard.Gap();

            if (listingStandard.ButtonText("Close".Translate()))
            {
                Close();
            }

            listingStandard.End();
        }
    }
}
