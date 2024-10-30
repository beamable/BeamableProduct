using UnityEditor;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		static BeamGUI()
		{
			EditorApplication.update += OnUpdate;
		}

		private static void OnUpdate()
		{
			DropdownUpdate();
		}
	}
}
