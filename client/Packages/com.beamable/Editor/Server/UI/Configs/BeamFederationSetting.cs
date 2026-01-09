using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
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
		private const string NO_FEDEDERATIONS_FOUND_INFO = "No Federation Implementation found. Make sure that your Microservice is implementing any federation interface with your Federation ID. You can create a new federation ID one by clicking on:";
		private Dictionary<string, bool> _foldoutStates = new();

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.singleLineHeight;
			
			if (!_foldoutStates.TryGetValue(property.propertyPath, out bool state) || !state)
				return height;
			
			var entriesCount = property.FindPropertyRelative(nameof(BeamFederationSetting.entries)).arraySize;
			
			if (entriesCount == 0)
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
			height += entriesCount * EditorGUIUtility.singleLineHeight;
			
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
			
			var entriesProperty = property.FindPropertyRelative(nameof(BeamFederationSetting.entries));
			
			if (entriesProperty.arraySize == 0)
			{
				var noFederationContent = new GUIContent(NO_FEDEDERATIONS_FOUND_INFO);
				GUIStyle guiStyle = new GUIStyle(EditorStyles.label)
				{
					wordWrap = true,
					richText = true,
				};
				
				const int indentWidth = 15;
				position.height = guiStyle.CalcHeight(noFederationContent, EditorGUI.IndentedRect(position).width);
				EditorGUI.LabelField(position, noFederationContent, guiStyle);
				var linkStyle = new GUIStyle(EditorStyles.linkLabel)
				{
					richText = true, 
					margin = new RectOffset(indentWidth * (EditorGUI.indentLevel + 1), 0,0,0),
					alignment = TextAnchor.MiddleLeft,
				};
				if (BeamGUI.LinkButton(new GUIContent("<b>Create->Federation Id</b>"), linkStyle))
				{
					var usamWindow = EditorWindow.GetWindow<UsamWindow>();
					if(usamWindow == null)
					{
						UsamWindow.CreateInState(UsamWindow.WindowState.CREATE_FEDERATION_ID);
						return;
					}
					usamWindow.state = UsamWindow.WindowState.CREATE_FEDERATION_ID;
				}
				EditorGUI.indentLevel--;
				EditorGUI.EndProperty();
				return;
			}

			for (int j = 0; j < entriesProperty.arraySize; j++)
			{
				var entry = entriesProperty.GetArrayElementAtIndex(j);
				string federationClassName = entry.FindPropertyRelative(nameof(BeamFederationEntry.federationClassName)).stringValue;
				string interfaceName = entry.FindPropertyRelative(nameof(BeamFederationEntry.interfaceName)).stringValue;
				EditorGUI.LabelField(position, $"• {interfaceName}<{federationClassName}>");
				position.y += EditorGUIUtility.singleLineHeight;
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
