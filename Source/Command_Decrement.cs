using UnityEngine;
using Verse;

namespace FarmingHysteresis
{
	internal class Command_Decrement : Command_Action
	{
		public override GizmoResult GizmoOnGUI(Vector2 loc, float maxWidth)
		{
			GizmoResult result = base.GizmoOnGUI(loc, maxWidth);
			Rect rect = new Rect(loc.x, loc.y, GetWidth(maxWidth), 75f);
			Rect position = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
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
}
