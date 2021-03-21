using UnityEngine;
using Verse;

namespace FarmingHysteresis
{
	internal class Command_Increment : Command_Action
	{
		public override GizmoResult GizmoOnGUI(Vector2 loc, float maxWidth)
		{
			GizmoResult result = base.GizmoOnGUI(loc, maxWidth);
			Rect rect = new Rect(loc.x, loc.y, GetWidth(maxWidth), 75f);
			Rect position = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
			Texture2D image = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");

			GUI.DrawTexture(position, image);
			return result;
		}
	}
}
