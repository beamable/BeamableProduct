using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		void OnCommandsGui()
		{
			GUILayout.Label($"Fill in command data here, there are {_history.commands.Count}");
		}
	}
}
