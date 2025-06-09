using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{
		
		
		
		public int DrawBadges(BeamManifestEntryFlags flags)
		{
			var iconStyle = new GUIStyle {margin = new RectOffset(12, 0, 3, 0)};
			var badgeCount = 0;
			
			if (flags.HasFlagType(BeamManifestEntryFlags.IS_SERVICE))
			{ // draw the service icon
				var iconRect = GUILayoutUtility.GetRect(GUIContent.none, iconStyle,
				                                        GUILayout.Width(toolbarHeight - 12),
				                                        GUILayout.Height(toolbarHeight - 12));
				GUI.DrawTexture(iconRect, BeamGUI.iconService, ScaleMode.ScaleToFit);
				GUI.Label(iconRect, new GUIContent(null, null, "This is a local service"), GUIStyle.none);
				badgeCount++;
			}
			
			if (flags.HasFlagType(BeamManifestEntryFlags.IS_STORAGE))
			{
				var iconRect = GUILayoutUtility.GetRect(GUIContent.none, iconStyle,
				                                        GUILayout.Width(toolbarHeight - 12),
				                                        GUILayout.Height(toolbarHeight - 12));
				GUI.DrawTexture(iconRect, BeamGUI.iconStorage, ScaleMode.ScaleToFit);
				GUI.Label(iconRect, new GUIContent(null, null, "This is a local storage"), GUIStyle.none);
				badgeCount++;
			}
			
			if (flags.HasFlagType(BeamManifestEntryFlags.IS_READONLY))
			{ // draw the readonly-badge
				var iconRect = GUILayoutUtility.GetRect(GUIContent.none, iconStyle,
				                                        GUILayout.Width(toolbarHeight - 12),
				                                        GUILayout.Height(toolbarHeight - 12));
				var c = GUI.color;
				GUI.color = new Color(1, 1, 1, .8f);
				int extraSize = 2;
				GUI.DrawTexture(new Rect(iconRect.x - extraSize, iconRect.y - extraSize, iconRect.width + extraSize*2, iconRect.height + extraSize*2), BeamGUI.iconLocked, ScaleMode.ScaleToFit);
				GUI.color = c;
				GUI.Label(iconRect, new GUIContent(null, null, "This service is readonly"), GUIStyle.none);
				badgeCount++;
			}

			return badgeCount;
		}

	}
}
