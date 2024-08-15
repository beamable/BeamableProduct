using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		public Vector2 scrollerPositionServerEvents;
		
		void OnServerGui()
		{
			GUILayout.Label($"DEBUG SERVER!");
			
			DrawTools(new CliWindowToolAction
			{
				name = "clear",
				onClick = () =>
				{
					_history.serverEvents.Clear();
				}
			});
			

			scrollerPositionServerEvents = GUILayout.BeginScrollView(scrollerPositionServerEvents);
			{
				for (var i = 0; i < _history.serverEvents.Count; i++)
				{
					var evt = _history.serverEvents[i];
					GUILayout.Label($"[{evt.time:00}] server event - {evt.message}");
				}
			}
			GUILayout.EndScrollView();

		}
	}
}
