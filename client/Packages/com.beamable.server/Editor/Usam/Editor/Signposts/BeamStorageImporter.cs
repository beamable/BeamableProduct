using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	
	[ScriptedImporter(0, ".beamstorage")]
	public class BeamStorageImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			var filePath = ctx.assetPath;

			var contents = File.ReadAllText(filePath);

			var instance = ScriptableObject.CreateInstance<BeamStorageAssetJson>();
			instance.fileName = ctx.assetPath;
			instance.json = contents;
			ctx.AddObjectToAsset("data", instance);
			ctx.SetMainObject(instance);
		}
	}

	[CustomEditor(typeof(BeamStorageImporter))]
	public class BeamStorageEditor : ScriptedImporterEditor
	{
		public override bool showImportedObject => false;

		public int oldInstanceId = -1;
		private SerializedObject _sob;
		private BeamStorageAsset _storageAsset;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			if (assetTarget is BeamStorageAssetJson assetJson)
			{
				var instanceId = assetJson.GetInstanceID();
				if (oldInstanceId != instanceId)
				{
					oldInstanceId = instanceId;

					_storageAsset ??= ScriptableObject.CreateInstance<BeamStorageAsset>();
					_storageAsset.data ??= new BeamStorageSignpost();
					JsonUtility.FromJsonOverwrite(assetJson.json, _storageAsset.data);
					_sob = new SerializedObject(_storageAsset);

				}

				if (_sob != null)
				{
					var rootProperty = _sob.FindProperty(nameof(BeamStorageAsset.data));
					var property = rootProperty;
					property.NextVisible(true);

					do
					{
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField(property);

						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(assetJson, "Change Beam Storage");
							_sob.ApplyModifiedPropertiesWithoutUndo();
							var json = JsonUtility.ToJson(_storageAsset.data, prettyPrint: true);

							File.WriteAllText(assetJson.fileName, json);
							EditorUtility.SetDirty(target);
							assetJson.json = json;
							_sob = new SerializedObject(_storageAsset);
						}
					} while (property.NextVisible(false));

				}
			}
		}
	}
}


