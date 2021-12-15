
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.Environment;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content.UI
{
	[CustomEditor(typeof(ApiContent), true)]
	public class ReadonlyUntilMicroserviceDrawer : UnityEditor.Editor
	{
		private Promise<BeamablePackageMeta> _hasPackage;

		public ReadonlyUntilMicroserviceDrawer()
		{
			_hasPackage = BeamablePackages.GetServerPackage();
		}

		public override void OnInspectorGUI()
		{
			var package = (_hasPackage ?? (_hasPackage = BeamablePackages.GetServerPackage())).GetResult();

			if (package?.IsPackageAvailable ?? false)
			{
				base.OnInspectorGUI();
			}
			else
			{
				EditorGUILayout.LabelField("This feature is meant to be used with the Microservices package. Please download the package from the Toolbox", EditorStyles.wordWrappedLabel);
				var wasEnabled = GUI.enabled;
				GUI.enabled = false;
				base.OnInspectorGUI();
				GUI.enabled = wasEnabled;
			}

		}
	}
}
