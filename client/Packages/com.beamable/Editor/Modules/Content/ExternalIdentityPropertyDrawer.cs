using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
	[CustomPropertyDrawer(typeof(ExternalIdentity))]
	public class ExternalIdentityPropertyDrawer : PropertyDrawer
	{
		private const int PADDING = 2;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 3 + PADDING * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			List<string> identities = GetIdentitiesOptions();

			SerializedProperty serviceProperty = property.FindPropertyRelative(nameof(ExternalIdentity.Service));
			serviceProperty.stringValue = "BlockchainFederationService";

			SerializedProperty identityProperty = property.FindPropertyRelative(nameof(ExternalIdentity.Namespace));

			Rect nextRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + PADDING,
									 position.width, EditorGUIUtility.singleLineHeight);

			var identitiesGuiContents = identities.Select(d => new GUIContent(d)).ToList();

			var selectedIndex = identities.FindIndex(d => d.Equals(identityProperty.stringValue));

			if (selectedIndex == -1)
			{
				if (string.IsNullOrEmpty(identityProperty.stringValue))
				{
					selectedIndex = 0;
				}
			}

			EditorGUI.BeginChangeCheck();
			var nextServiceIndex = EditorGUI.Popup(nextRect, new GUIContent("External identity"), selectedIndex,
												   identitiesGuiContents.ToArray(), EditorStyles.popup);

			if (EditorGUI.EndChangeCheck())
			{
				identityProperty.stringValue =
					identities.FirstOrDefault(i => i.Equals(identitiesGuiContents[nextServiceIndex].text));
			}
		}

		private List<string> GetIdentitiesOptions()
		{
			Type assignableType = typeof(IThirdPartyCloudIdentity);

			List<Type> types = AppDomain.CurrentDomain.GetAssemblies().ToList().SelectMany(x => x.GetTypes())
										.Where(t => assignableType.IsAssignableFrom(t) && t.IsClass).ToList();

			List<string> list = new List<string> { "None" };

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
