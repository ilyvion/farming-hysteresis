namespace FarmingHysteresis;

internal class Command_Decrement : Command_ArrowOverlayAction
{
    // The arrow ordinarily points up. So let's render it upside down.
    protected override float ArrowRotationDegrees => 180f;
}
