﻿using Beamable.Common.Content;
using System;
using System.Linq;
using UnityEngine;

namespace Beamable.Content
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(
		fileName = "Content Configuration",
		menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
				  "Content Configuration")]
#endif
	public class ContentConfiguration : ModuleConfigurationObject
	{
		public static ContentConfiguration Instance => Get<ContentConfiguration>();

		[Tooltip("Multiple content namespaces are an advanced feature. You can deploy content to a parallel content namespace. You will need to write code in your game to decide which namespace to use. Beamable features like Commerce, Inventory, or Events will only work with content deployed to the \"global\" namespace. ")]
		[ReadonlyIf(nameof(multipleContentNamespacesSettingLocked), negate = false)]
		public bool EnableMultipleContentNamespaces = false;

		[HideInInspector]
		public bool multipleContentNamespacesSettingLocked;

#if UNITY_EDITOR
        [HideInInspector]
        public string EditorManifestID = BeamableConstants.DEFAULT_MANIFEST_ID;
#endif

		[Tooltip("This is the starting manifest ID that will be used when the game starts up. If you change it here, the Beamable API will initialize with content from this namespace. You can also update the setting at runtime.")]
		[SerializeField, ReadonlyIf(nameof(EnableMultipleContentNamespaces), negate = true, specialDrawer = ReadonlyIfAttribute.SpecialDrawer.DelayedString)]
		private string _runtimeManifestID = BeamableConstants.DEFAULT_MANIFEST_ID;
		public string RuntimeManifestID
		{
			get => ValidateManifestID(_runtimeManifestID);
			set => _runtimeManifestID = ValidateManifestID(value);
		}

		[Header("Baking")]
		[Tooltip("Create zip archive of content upon baking. Makes first content resolve call longer due to decompression.")]
		public bool EnableBakedContentCompression = true;

#if UNITY_STANDALONE
        [Tooltip("Re-bake content on each build. This option is available only on Standalone build target.")]
        public bool BakeContentOnBuild = true;
#endif

		public ContentParameterProvider ParameterProvider
		{
			get
			{
				var manifestID = RuntimeManifestID;
				if (!EnableMultipleContentNamespaces && manifestID != BeamableConstants.DEFAULT_MANIFEST_ID)
				{
					Debug.LogWarning($"Beamable API is using manifest with id '{manifestID}' while manifest namespaces feature is disabled!");
				}
				return new ContentParameterProvider()
				{
					manifestID = manifestID
				};
			}
		}

		private void OnValidate()
		{
#if UNITY_EDITOR
            if (!IsValidManifestID(EditorManifestID, out var message))
            {
                Debug.LogWarning($"Invalid manifest ID: {message}");
                EditorManifestID = BeamableConstants.DEFAULT_MANIFEST_ID;
            }
#endif

			if (!EnableMultipleContentNamespaces)
			{
				_runtimeManifestID = BeamableConstants.DEFAULT_MANIFEST_ID;
			}
			else if (!IsValidManifestID(_runtimeManifestID, out var runtimeMessage))
			{
				Debug.LogWarning($"Invalid runtime manifest ID: {runtimeMessage}");
				_runtimeManifestID = BeamableConstants.DEFAULT_MANIFEST_ID;
			}
		}
		public static bool IsValidManifestID(string name, out string message)
		{
			const int MAX_NAME_LENGTH = 36;

			message = null;
			if (string.IsNullOrWhiteSpace(name))
			{
				message = "Name can not be empty string.";
				return false;
			}

			if (name.Length > MAX_NAME_LENGTH)
			{
				message = $"Name can not be longer then {MAX_NAME_LENGTH.ToString()} characters.";
				return false;
			}

			bool TestChar(Char c)
			{
				return char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.';
			}

			if (!name.All(TestChar))
			{
				message = "Name can contain only letters, digits, '-', '_' and '.'.";
				return false;
			}
			return true;
		}

		public string ValidateManifestID(string id)
		{
			return IsValidManifestID(id, out var _)
				? id
				: BeamableConstants.DEFAULT_MANIFEST_ID;
		}
	}
}
