using UnityEditor;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		void OnOverridesGui()
		{
			var x = EditorGUILayout.IntField("Server Log Reload Cap", _history.serverLogCap);
		}
	}
}
