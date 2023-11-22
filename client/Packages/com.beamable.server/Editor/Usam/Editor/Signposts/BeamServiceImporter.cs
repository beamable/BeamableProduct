using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[ScriptedImporter(0, ".beamservice")]
	public class BeamServiceImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			var filePath = ctx.assetPath;

			var contents = File.ReadAllText(filePath);


			var instance = ScriptableObject.CreateInstance<BeamServiceAssetJson>();
			instance.fileName = ctx.assetPath;
			instance.json = contents;
			ctx.AddObjectToAsset("data", instance);
			ctx.SetMainObject(instance);

		}

	}

	[CustomEditor(typeof(BeamServiceImporter))]
	public class BeamServiceEditor : ScriptedImporterEditor
	{
		public override bool showImportedObject => false;

		public int oldInstanceId = -1;
		private SerializedObject _sob;
		private BeamServiceAsset _serviceAsset;

		public override void OnInspectorGUI()
		{

			base.OnInspectorGUI();

			if (assetTarget is BeamServiceAssetJson assetJson)
			{
				var instanceId = assetJson.GetInstanceID();
				if (oldInstanceId != instanceId)
				{
					oldInstanceId = instanceId;

					_serviceAsset ??= ScriptableObject.CreateInstance<BeamServiceAsset>();
					_serviceAsset.data ??= new BeamServiceSignpost();
					JsonUtility.FromJsonOverwrite(assetJson.json, _serviceAsset.data);
					_sob = new SerializedObject(_serviceAsset);

				}

				if (_sob != null)
				{
					var rootProperty = _sob.FindProperty(nameof(BeamServiceAsset.data));
					var property = rootProperty;
					property.NextVisible(true);

					do
					{
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField(property);



						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(assetJson, "Change Beam Service");
							_sob.ApplyModifiedPropertiesWithoutUndo();
							var json = JsonUtility.ToJson(_serviceAsset.data, prettyPrint: true);

							File.WriteAllText(assetJson.fileName, json);
							EditorUtility.SetDirty(target);
							assetJson.json = json;
							_sob = new SerializedObject(_serviceAsset);
						}
					} while (property.NextVisible(false));

				}
			}



		}

	}
}
