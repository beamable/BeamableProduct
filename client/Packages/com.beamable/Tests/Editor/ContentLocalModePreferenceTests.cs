using System;
using System.IO;
using System.Reflection;
using Beamable.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class ContentLocalModePreferenceTests
	{
		private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
		private static string Key(string path) => (string)typeof(ContentConfiguration)
			.GetMethod("LocalContentPreferenceKeyFor", StaticPrivate).Invoke(null, new object[] { path });

		[Test]
		public void Preference_is_scoped_to_project_path_and_normalizes_equivalent_paths()
		{
			var root = Path.Combine(Path.GetTempPath(), "BeamableLocalModePreferenceTests");
			Assert.That(Key(Path.Combine(root, "A", "Assets")), Is.Not.EqualTo(Key(Path.Combine(root, "B", "Assets"))));
			Assert.That(Key(Path.Combine(root, "A", "Assets")), Is.EqualTo(Key(Path.Combine(root, "A", "Assets", "..", "Assets"))));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Upgrade_preserves_choice_once_then_asset_changes_cannot_override_it(bool legacyEnabled)
		{
			var path = "Assets/__LocalModePreference_" + Guid.NewGuid().ToString("N") + ".asset";
			var key = Key(Application.dataPath);
			var hadValue = EditorPrefs.HasKey(key);
			var oldValue = EditorPrefs.GetBool(key);
			var configuration = ScriptableObject.CreateInstance<ContentConfiguration>();
			try
			{
				EditorPrefs.DeleteKey(key);
				var serialized = new SerializedObject(configuration);
				serialized.FindProperty("_legacyLocalContentInEditor").boolValue = legacyEnabled;
				serialized.ApplyModifiedPropertiesWithoutUndo();
				AssetDatabase.CreateAsset(configuration, path);
				// Unity produces the asset. Use the historical field name independently established
				// by client/Assets/Beamable/Resources/ContentConfiguration.asset, then really import it.
				var legacyAsset = File.ReadAllText(path).Replace("_legacyLocalContentInEditor:", "EnableLocalContentInEditor:");
				Assert.That(legacyAsset, Does.Contain("EnableLocalContentInEditor: " + (legacyEnabled ? "1" : "0")));
				Resources.UnloadAsset(configuration);
				configuration = null;
				File.WriteAllText(path, legacyAsset);
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				configuration = AssetDatabase.LoadAssetAtPath<ContentConfiguration>(path);
				Assert.That(new SerializedObject(configuration).FindProperty("_legacyLocalContentInEditor").boolValue, Is.EqualTo(legacyEnabled), "Unity must import the former field name.");
				Assert.That(configuration.EnableLocalContentInEditor, Is.EqualTo(legacyEnabled), "Upgrading must not implicitly leave Local Mode.");
				Assert.That(EditorPrefs.HasKey(key), Is.True);
				var before = File.ReadAllBytes(path);
				configuration.EnableLocalContentInEditor = true;
				var parameters = configuration.ParameterProvider;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				configuration = AssetDatabase.LoadAssetAtPath<ContentConfiguration>(path);
				Assert.That(configuration.EnableLocalContentInEditor, Is.True);
				EditorJsonUtility.FromJsonOverwrite("{\"_legacyLocalContentInEditor\":false}", configuration);
				Assert.That(configuration.EnableLocalContentInEditor, Is.True);
				Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
				configuration.EnableLocalContentInEditor = false;
				EditorJsonUtility.FromJsonOverwrite("{\"_legacyLocalContentInEditor\":true}", configuration);
				Assert.That(configuration.EnableLocalContentInEditor, Is.False, "A teammate's later asset change cannot opt this developer in.");
				Assert.That(parameters.EnableLocalContentInEditor, Is.True, "A captured runtime configuration must not change mid-session.");
				Assert.That(configuration.ParameterProvider.EnableLocalContentInEditor, Is.False);
			}
			finally
			{
				AssetDatabase.DeleteAsset(path);
				if (configuration != null) UnityEngine.Object.DestroyImmediate(configuration);
				if (hadValue) EditorPrefs.SetBool(key, oldValue); else EditorPrefs.DeleteKey(key);
			}
		}
	}
}
