namespace FarmingHysteresis;

internal abstract class Command_ArrowOverlayAction : Command_Action
{
    protected abstract float ArrowRotationDegrees { get; }

    public override GizmoResult GizmoOnGUI(Vector2 loc, float maxWidth, GizmoRenderParms parms)
    {
        var result = base.GizmoOnGUI(loc, maxWidth, parms);
        Rect rect = new(loc.x, loc.y, GetWidth(maxWidth), 75f);
        Rect position = new(
            rect.x + rect.width - GenUI.SmallIconSize,
            rect.y,
            GenUI.SmallIconSize,
            GenUI.SmallIconSize
        );
        var image = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");

        if (ArrowRotationDegrees == 0f)
        {
            GUI.DrawTexture(position, image);
        }
        else
        {
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            var center = GUIUtility.GUIToScreenPoint(position.center);
            UI.RotateAroundPivot(ArrowRotationDegrees, center);
            GUI.DrawTexture(position, image);
            GUI.matrix = old;
        }

        return result;
    }
}
