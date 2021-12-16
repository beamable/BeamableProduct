
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.Environment;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content.UI
{
	[CustomEditor(typeof(ApiContent), true)]
	public class ReadonlyUntilMicroserviceEditor : UnityEditor.Editor
	{
		private Promise<BeamablePackageMeta> _packagePromise;

		private BeamablePackageMeta Package =>  (_packagePromise ?? (_packagePromise = BeamablePackages.GetServerPackage())).GetResult();

		public ReadonlyUntilMicroserviceEditor()
		{
			_packagePromise = BeamablePackages.GetServerPackage();
		}

		public override void OnInspectorGUI()
		{
			var package = Package;
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
