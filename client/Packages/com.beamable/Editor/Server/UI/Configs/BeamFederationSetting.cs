using Beamable.Editor.BeamCli.Commands;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	[Serializable]
	public class BeamFederationSetting
	{
		// UI-state can live here.
		public List<BeamFederationEntry> entries = new ();

		public bool IsValid()
		{
			if (entries == null) return false;
			if (entries.Any(item => string.IsNullOrEmpty(item.federationId))) return false;

			return true;
		}
	}


	[CustomPropertyDrawer(typeof(BeamFederationSetting))]
	public class FederationEntryPropertyDraw : PropertyDrawer
	{
		private const string NO_FEDEDERATIONS_FOUND_INFO = "No Federation Implementation found. Make sure that your Microservice is implementing any federation interface with your Federation ID. You can create a new federation ID one by clicking on <b>Create->Federation Id</b>.";
		private Dictionary<string, bool> _foldoutStates = new();

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.singleLineHeight;
			
			if (!_foldoutStates.TryGetValue(property.propertyPath, out bool state) || !state)
				return height;

			height += EditorGUIUtility.singleLineHeight;
			
			var groupedFederations = GroupFederations(property);

			if (groupedFederations.Count == 0)
			{
				var noFedInfoContent = new GUIContent(NO_FEDEDERATIONS_FOUND_INFO);
				GUIStyle guiStyle = new GUIStyle(EditorStyles.label)
				{
					wordWrap = true,
					richText = true
				};
				float indentPaddingSpace = 15f;
				height += guiStyle.CalcHeight(noFedInfoContent, EditorGUIUtility.currentViewWidth - EditorGUI.indentLevel * indentPaddingSpace);
				return height;
			}

			height += groupedFederations.Select(kvp =>
			{
				float groupHeight = EditorGUIUtility.singleLineHeight; // Federation ID foldout
				if (_foldoutStates.TryGetValue(kvp.Key, out bool foldoutState) && foldoutState)
				{
					groupHeight += EditorGUIUtility.singleLineHeight; // Interfaces that uses it title
					groupHeight += groupedFederations.Values.Count * EditorGUIUtility.singleLineHeight; // For each interface
				}

				return groupHeight;
			}).Sum();

			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			
			var boldLabel = new GUIStyle(EditorStyles.foldout)
			{
				fontStyle = FontStyle.Bold
			};

			position.height = EditorGUIUtility.singleLineHeight;
			
			_foldoutStates.TryAdd(property.propertyPath, true);
			_foldoutStates[property.propertyPath] = EditorGUI.Foldout(position, _foldoutStates[property.propertyPath], label, true, boldLabel);

			if (!_foldoutStates[property.propertyPath])
			{
				EditorGUI.EndProperty();
				return;
			}
			
			position.y += EditorGUIUtility.singleLineHeight;
			EditorGUI.indentLevel++;

			var groupedFederations = GroupFederations(property);

			if (groupedFederations.Count == 0)
			{
				var noFederationContent = new GUIContent(NO_FEDEDERATIONS_FOUND_INFO);
				GUIStyle guiStyle = new GUIStyle(EditorStyles.label)
				{
					wordWrap = true,
					richText = true,
				};
				position.height = guiStyle.CalcHeight(noFederationContent, EditorGUI.IndentedRect(position).width);
				EditorGUI.LabelField(position, noFederationContent, guiStyle);
				EditorGUI.indentLevel--;
				EditorGUI.EndProperty();
				return;
			}

			foreach ((string federationId, List<string> interfaces) in groupedFederations)
			{
				string foldoutKey = federationId + property.propertyPath;
				
				_foldoutStates.TryAdd(foldoutKey, true);
				position.height = EditorGUIUtility.singleLineHeight;
				_foldoutStates[foldoutKey] = EditorGUI.Foldout(
					position,
					_foldoutStates[foldoutKey],
					federationId,
					true);

				position.y += EditorGUIUtility.singleLineHeight;

				if (!_foldoutStates[foldoutKey])
				{
					continue;
				}
				
				EditorGUI.indentLevel++;
				EditorGUI.LabelField(position,"Interfaces that uses it:", EditorStyles.boldLabel);
				position.y += EditorGUIUtility.singleLineHeight;
				foreach (string interfaceName in interfaces)
				{
					position.height = EditorGUIUtility.singleLineHeight;
					EditorGUI.LabelField(position, $" - {interfaceName}");
					position.y += EditorGUIUtility.singleLineHeight;
				}

				EditorGUI.indentLevel--;
			}
			EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}

		private Dictionary<string, List<string>> GroupFederations(SerializedProperty setting)
		{
			var groups = new Dictionary<string, List<string>>();
			
			var entriesProperty = setting.FindPropertyRelative(nameof(BeamFederationSetting.entries));

			for (int j = 0; j < entriesProperty.arraySize; j++)
			{
				var entry = entriesProperty.GetArrayElementAtIndex(j);
				string federationId = entry.FindPropertyRelative(nameof(BeamFederationEntry.federationId)).stringValue;
				string interfaceName = entry.FindPropertyRelative(nameof(BeamFederationEntry.interfaceName)).stringValue;

				if (string.IsNullOrEmpty(federationId) || string.IsNullOrEmpty(interfaceName))
					continue;

				if (!groups.ContainsKey(federationId))
				{
					groups[federationId] = new List<string>();
				}

				groups[federationId].Add(interfaceName);
			}

			return groups;
		}
	}

}
