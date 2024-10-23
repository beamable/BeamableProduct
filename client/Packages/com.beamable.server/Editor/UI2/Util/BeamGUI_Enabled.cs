using System;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		public static void ShowDisabled(bool enabled, Action onGui)
		{
			var wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled && enabled;
			try
			{
				onGui?.Invoke();
			}
			finally
			{
				GUI.enabled = wasEnabled;
			}
		}
	}
}
