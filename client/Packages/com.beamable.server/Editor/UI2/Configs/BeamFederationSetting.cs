using Beamable.Editor.BeamCli.Commands;
using Beamable.Server.Editor.Usam;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.Configs
{
	[Serializable]
	public class BeamFederationSetting
	{
		// UI-state can live here.
		public BeamFederationEntry entry = new BeamFederationEntry();

		public bool IsValid()
		{
			if (entry == null) return false;
			if (string.IsNullOrEmpty(entry.federationId)) return false;

			return true;
		}
	}


	[CustomPropertyDrawer(typeof(BeamFederationSetting))]
	public class FederationEntryPropertyDraw : PropertyDrawer
	{
		private const int warningHeight = 20;
		private float _dynamicHeight =  EditorGUIUtility.singleLineHeight;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return _dynamicHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var usam = BeamEditorContext.Default.ServiceScope.GetService<UsamService>();

			if (usam?.latestManifest == null)
			{
				return;
			}

			var options = BeamableMicroservicesSettings.availableFederationIds;
			_dynamicHeight = EditorGUIUtility.singleLineHeight;
			if (options.Count == 0)
			{
				_dynamicHeight += warningHeight;
				EditorGUI.HelpBox(position, "A Federation Id is required to add federations to existing service. You can create a new one by clicking on Create->Federation Id.",MessageType.Warning);
			}
			else
			{
				var federationIdProp =
					property.FindPropertyRelative(
						$"{nameof(BeamFederationSetting.entry)}.{nameof(BeamFederationEntry.federationId)}");
				var federationTypeProp =
					property.FindPropertyRelative(
						$"{nameof(BeamFederationSetting.entry)}.{nameof(BeamFederationEntry.interfaceName)}");

				var width = EditorGUIUtility.labelWidth;

				var typeRect = new Rect(position.x + 1, position.y + 1, width - 2, EditorGUIUtility.singleLineHeight);
				var idRect = new Rect(typeRect.xMax + 4, typeRect.y, position.width - width - 4,
				                      EditorGUIUtility.singleLineHeight);

				var types = usam.latestManifest.availableFederationTypes.ToArray();
				var selectedType = Array.IndexOf(types, federationTypeProp.stringValue);
				var nextSelectedType = EditorGUI.Popup(typeRect, selectedType, types);
				if (nextSelectedType < 0) nextSelectedType = 0;
				federationTypeProp.stringValue = types[nextSelectedType];

				var selectedId = options.IndexOf(federationIdProp.stringValue);
				var nextSelectedId = EditorGUI.Popup(idRect, selectedId, options.ToArray());
				if (nextSelectedId < 0) nextSelectedId = 0;
				federationIdProp.stringValue = options[nextSelectedId];
			}
			
		}
	}

}
