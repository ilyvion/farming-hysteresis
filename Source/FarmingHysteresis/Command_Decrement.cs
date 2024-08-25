namespace FarmingHysteresis;

internal class Command_Decrement : Command_Action
{
    public override GizmoResult GizmoOnGUI(Vector2 loc, float maxWidth, GizmoRenderParms parms)
    {
        GizmoResult result = base.GizmoOnGUI(loc, maxWidth, parms);
        Rect rect = new(loc.x, loc.y, GetWidth(maxWidth), 75f);
        Rect position = new(rect.x + rect.width - GenUI.SmallIconSize, rect.y, GenUI.SmallIconSize, GenUI.SmallIconSize);
        Texture2D image = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");

        // The arrow ordinarily points up. So let's render it upside down.
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        Vector2 vector = position.center;
        Vector2 center = GUIUtility.GUIToScreenPoint(vector);
        UI.RotateAroundPivot(180, center);
        GUI.DrawTexture(position, image);
        GUI.matrix = old;

        return result;
    }
}
