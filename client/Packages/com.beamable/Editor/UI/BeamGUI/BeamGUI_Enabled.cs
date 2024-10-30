using Beamable.Common;
using System;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		public static void ShowDisabled(bool enabled, Action onGui)
		{
			ShowDisabled<Unit>(enabled, () =>
			{
				onGui?.Invoke();
				return PromiseBase.Unit;
			});
		}
		public static T ShowDisabled<T>(bool enabled, Func<T> onGui)
		{
			var wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled && enabled;
			try
			{
				return onGui.Invoke();
			}
			finally
			{
				GUI.enabled = wasEnabled;
			}
		}
	}
}
