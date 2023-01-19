using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Content;

namespace Beamable.Server.Editor
{
	[CustomPropertyDrawer(typeof(Federation))]
	public class FederationPropertyDrawer : PropertyDrawer
	{
		private const int PADDING = 2;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 3 + PADDING * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			var descriptors = serviceRegistry.Descriptors;

			if (descriptors.Count == 0)
			{
				position = EditorGUI.PrefixLabel(position, label);
				EditorGUI.SelectableLabel(position, "You must create a Microservice to configure a Federation",
				                          EditorStyles.wordWrappedLabel);
				return;
			}

			var routeInfoPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(routeInfoPosition, "Federation",
			                     new GUIStyle(EditorStyles.label) {font = EditorStyles.boldFont});
			EditorGUI.indentLevel += 1;

			var servicesGuiContents = descriptors
			                          .Select(d => new GUIContent(d.Name))
			                          .ToList();

			var nextRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + PADDING,
			                        position.width, EditorGUIUtility.singleLineHeight);

			SerializedProperty serviceProperty = property.FindPropertyRelative(nameof(Federation.Service));
			var originalServiceIndex = descriptors.FindIndex(d => d.Name.Equals(serviceProperty.stringValue));

			if (originalServiceIndex == -1)
			{
				if (string.IsNullOrEmpty(serviceProperty.stringValue))
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
			var nextServiceIndex = EditorGUI.Popup(nextRect, new GUIContent("Microservice"), originalServiceIndex,
			                                       servicesGuiContents.ToArray(), EditorStyles.popup);
			if (EditorGUI.EndChangeCheck())
			{
				serviceProperty.stringValue = descriptors
				                              .FirstOrDefault(descriptor =>
					                                              descriptor.Name.Equals(
						                                              servicesGuiContents[nextServiceIndex].text))
				                              ?.Name;
			}

			List<string> identities = GetIdentitiesOptions();

			SerializedProperty namespaceProperty = property.FindPropertyRelative(nameof(Federation.Namespace));
			nextRect = new Rect(position.x, position.y + 2 * EditorGUIUtility.singleLineHeight + PADDING,
			                    position.width, EditorGUIUtility.singleLineHeight);

			var identitiesGuiContents = identities.Select(d => new GUIContent(d)).ToList();
			var selectedNamespaceIndex = identities.FindIndex(d => d.Equals(namespaceProperty.stringValue));

			if (selectedNamespaceIndex == -1)
			{
				if (string.IsNullOrEmpty(namespaceProperty.stringValue))
				{
					selectedNamespaceIndex = 0;
				}
			}

			EditorGUI.BeginChangeCheck();
			var nextNamespaceIndex = EditorGUI.Popup(nextRect, new GUIContent("Namespace"),
			                                         selectedNamespaceIndex,
			                                         identitiesGuiContents.ToArray(), EditorStyles.popup);

			if (EditorGUI.EndChangeCheck())
			{
				namespaceProperty.stringValue =
					identities.FirstOrDefault(i => i.Equals(identitiesGuiContents[nextNamespaceIndex].text));
			}
		}

		private List<string> GetIdentitiesOptions()
		{
			Type assignableType = typeof(IThirdPartyCloudIdentity);

			List<Type> types = AppDomain.CurrentDomain.GetAssemblies().ToList().SelectMany(x => x.GetTypes())
			                            .Where(t => assignableType.IsAssignableFrom(t) && t.IsClass).ToList();

			List<string> list = new List<string>();

			foreach (Type type in types)
			{
				if (FormatterServices.GetUninitializedObject(type) is IThirdPartyCloudIdentity identity)
				{
					list.Add(identity.UniqueName);
				}
			}

			return list;
		}
	}
}
