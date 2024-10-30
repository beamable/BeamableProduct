using Beamable.Server.Editor.Usam;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	[Serializable]
	public class BeamStorageDependencySetting
	{
		public string StorageName;
	}


	[CustomPropertyDrawer(typeof(BeamStorageDependencySetting))]
	public class StorageDependencyDrawer : PropertyDrawer
	{
		private int _selected;
		private Regex _regex = new Regex(".Array.data\\[([0-9]+)]");

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			//load all possible dependencies
			var codeService = BeamEditorContext
							  .Default.ServiceScope.GetService<UsamService>();
			var options = codeService.latestManifest.storages
									 .Select(sd => sd.beamoId).ToArray();

			var storageNameProperty = property.FindPropertyRelative(nameof(BeamStorageDependencySetting.StorageName));

			//Some stuff to get the index of this property in it's array
			string indexInArray = string.Empty;
			if (property.propertyPath.Contains("Array"))
			{
				indexInArray =
					_regex.Match(property.propertyPath).Groups[1].ToString();
			}

			var previousIndex = Array.IndexOf(options, storageNameProperty.stringValue);

			var index = EditorGUI.Popup(position, $"Element {indexInArray}", previousIndex, options);

			if (index >= 0)
			{
				storageNameProperty.stringValue = options[index];
			}
			else
			{
				storageNameProperty.stringValue = null;
			}
		}
	}
}
