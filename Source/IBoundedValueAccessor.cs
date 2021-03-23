namespace FarmingHysteresis
{
	internal interface IBoundedValueAccessor
	{
		int LowerBoundValueRaw { get; set; }
		int UpperBoundValueRaw { get; set; }
	}
}