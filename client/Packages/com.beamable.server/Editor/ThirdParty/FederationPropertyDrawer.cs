using Beamable.Common.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.UI;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Content;

namespace Beamable.Server.Editor
{
	[CustomPropertyDrawer(typeof(Federation))]
	public class FederationPropertyDrawer : PropertyDrawer
	{
		private const int PADDING = 2;

		private List<BeamManifestServiceEntry> _filteredServiceEntries;
		private readonly List<FederationOption> _options = new List<FederationOption>();

		private static readonly List<string> _allowedInterfaces = new List<string>() {"IFederatedInventory"};

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 3 + PADDING * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var usamService = BeamEditorContext.Default.ServiceScope.GetService<UsamService>();

			if (_filteredServiceEntries == null)
			{
				_filteredServiceEntries = usamService.latestManifest.services.FindAll(s => s.federations.Count > 0).ToList();
			}

			if (_filteredServiceEntries.Count == 0)
			{
				position = EditorGUI.PrefixLabel(position, label);
				EditorGUI.SelectableLabel(
					position,
					"You must have a Microservice implementing IFederatedInventory before configuring it into an item or currency.",
					EditorStyles.wordWrappedLabel);
				return;
			}

			BuildOptions(_filteredServiceEntries);

			var routeInfoPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(routeInfoPosition, "Federation",
								 new GUIStyle(EditorStyles.label) { font = EditorStyles.boldFont });
			EditorGUI.indentLevel += 1;

			var servicesGuiContents = _options
									  .Select(opt => new GUIContent(opt.ToString()))
									  .ToList();

			var nextRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + PADDING,
									position.width, EditorGUIUtility.singleLineHeight);

			SerializedProperty serviceProperty = property.FindPropertyRelative(nameof(Federation.Service));
			SerializedProperty namespaceProperty = property.FindPropertyRelative(nameof(Federation.Namespace));
			var originalServiceIndex = _options.FindIndex(opt => opt.Microservice == serviceProperty.stringValue &&
																 opt.Namespace == namespaceProperty.stringValue);

			if (originalServiceIndex == -1)
			{
				if (string.IsNullOrEmpty(serviceProperty.stringValue) ||
					string.IsNullOrEmpty(namespaceProperty.stringValue))
				{
					servicesGuiContents.Insert(0, new GUIContent("<none>"));
					originalServiceIndex = 0;
				}
				else
				{
					servicesGuiContents.Insert(0, new GUIContent(serviceProperty.stringValue));
					originalServiceIndex = 0;
					if (!serviceProperty.stringValue.EndsWith(MISSING_SUFFIX))
					{
						serviceProperty.stringValue += MISSING_SUFFIX;
					}
				}
			}

			EditorGUI.BeginChangeCheck();
			var nextServiceIndex = EditorGUI.Popup(nextRect, new GUIContent("Federation"), originalServiceIndex,
												   servicesGuiContents.ToArray(), EditorStyles.popup);
			if (EditorGUI.EndChangeCheck())
			{
				var option =
					_options.FirstOrDefault(opt => opt.ToString().Equals(servicesGuiContents[nextServiceIndex].text));
				serviceProperty.stringValue = option?.Microservice;
				namespaceProperty.stringValue = option?.Namespace;
			}
		}

		private void BuildOptions(List<BeamManifestServiceEntry> descriptors)
		{
			_options.Clear();

			foreach (var descriptor in descriptors)
			{
				foreach (var federation in descriptor.federations)
				{
					if(_allowedInterfaces.Contains(federation.interfaceName))
					{
						_options.Add(new FederationOption { Microservice = descriptor.beamoId, Namespace = federation.federationId });
						break;
					}
				}
			}
		}

		[System.Serializable]
		private class FederationOption
		{
			public string Microservice { get; set; }
			public string Namespace { get; set; }

			public override string ToString()
			{
				return $"{Microservice} / {Namespace}";
			}
		}
	}

	[CustomPropertyDrawer(typeof(Namespace))]
	public class NamespacePropertyDrawer : PropertyDrawer
	{
		private const int PADDING = 2;
		private const int OPTIONS_CELL_HEIGHT = 20;
		private const int OPTIONS_CELL_TOP_PADDING = 20;
		private const string INTERFACE_NAME = "IFederatedGameServer";

		private List<BeamManifestServiceEntry> _filteredServiceEntries;
		private int _previousIndex = -1;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 3 + PADDING * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var usamService = BeamEditorContext.Default.ServiceScope.GetService<UsamService>();
			var lastPosition = new Rect(position.x, position.y + OPTIONS_CELL_TOP_PADDING, position.width, OPTIONS_CELL_HEIGHT);


			if (_filteredServiceEntries == null)
			{
				_filteredServiceEntries = usamService.latestManifest.services.FindAll(s => (s.federations.Count > 0) &&
																	s.federations.Where(e => e.interfaceName.Equals(INTERFACE_NAME)).ToList().Count > 0).ToList();
			}

			if (_filteredServiceEntries.Count == 0)
			{
				position = EditorGUI.PrefixLabel(position, label);
				EditorGUI.SelectableLabel(
					position,
					"You must have a Microservice implementing IFederatedGameServer before configuring it into an item or currency.",
					EditorStyles.wordWrappedLabel);
				return;
			}

			SerializedProperty nameProperty = property.FindPropertyRelative(nameof(Namespace.Name));
			SerializedProperty hasValueProperty = property.FindPropertyRelative(nameof(OptionalNamespace.HasValue));

			var options = BuildOptions();

			var previousIndex = Array.IndexOf(options.ToArray(), nameProperty.stringValue);

			var index = EditorGUI.Popup(lastPosition, $"Federation Namespace: ", previousIndex, options);

			if (_previousIndex == index)
			{
				return;
			}

			_previousIndex = index;

			if (index >= 0)
			{
				nameProperty.stringValue = options[index];
				hasValueProperty.boolValue = true;
			}
			else
			{
				nameProperty.stringValue = "<none>";
				hasValueProperty.boolValue = false;
			}

			nameProperty.serializedObject.ApplyModifiedProperties();
		}

		private string[] BuildOptions()
		{
			var options = new List<string>();
			foreach (BeamManifestServiceEntry entry in _filteredServiceEntries)
			{
				foreach (var fed in entry.federations)
				{
					if (fed.interfaceName.Equals(INTERFACE_NAME))
					{
						options.Add(fed.federationId);
					}
				}
			}

			return options.Distinct().ToArray();
		}

	}
}
