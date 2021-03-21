namespace FarmingHysteresis
{
	internal enum LatchMode
	{
		// Not yet determined - unknown state
		Unknown,
		// Below lower bound - enabled
		BelowLowerBound,
		// Lower bound met, but not upper bound - enabled
		AboveLowerBoundEnabled,
		// Upper bound met, but not yet below lower bound - disabled
		AboveLowerBoundDisabled,
		// Above upper bound - disabled
		AboveUpperBound,
	}
}
