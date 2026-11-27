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
		private static void ResetCache() => typeof(ContentConfiguration)
			.GetMethod("ResetLocalContentPreferenceCache", StaticPrivate).Invoke(null, null);

		[Test]
		public void Preference_is_scoped_to_project_path_and_normalizes_equivalent_paths()
		{
			var root = Path.Combine(Path.GetTempPath(), "BeamableLocalModePreferenceTests");
			Assert.That(Key(Path.Combine(root, "A", "Assets")), Is.Not.EqualTo(Key(Path.Combine(root, "B", "Assets"))));
			Assert.That(Key(Path.Combine(root, "A", "Assets")), Is.EqualTo(Key(Path.Combine(root, "A", "Assets", "..", "Assets"))));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Missing_preference_is_off_and_shared_asset_values_never_apply(bool assetEnabled)
		{
			var path = "Assets/__LocalModePreference_" + Guid.NewGuid().ToString("N") + ".asset";
			var key = Key(Application.dataPath);
			var hadValue = EditorPrefs.HasKey(key);
			var oldValue = EditorPrefs.GetBool(key);
			var configuration = ScriptableObject.CreateInstance<ContentConfiguration>();
			try
			{
				EditorPrefs.DeleteKey(key);
				ResetCache();
				AssetDatabase.CreateAsset(configuration, path);
				// Unity produces the asset. Add the former field as an older checkout committed it
				// (name and format independently established by
				// client/Assets/Beamable/Resources/ContentConfiguration.asset), then really import it.
				var produced = File.ReadAllText(path);
				const string anchor = "  EnablePropertyOrderDependenceForContentChecksum:";
				Assert.That(produced, Does.Contain(anchor));
				var newline = produced.Contains("\r\n") ? "\r\n" : "\n";
				string WithFormerField(bool enabled) =>
					produced.Replace(anchor, "  EnableLocalContentInEditor: " + (enabled ? "1" : "0") + newline + anchor);
				Resources.UnloadAsset(configuration);
				configuration = null;
				File.WriteAllText(path, WithFormerField(assetEnabled));
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				configuration = AssetDatabase.LoadAssetAtPath<ContentConfiguration>(path);
				Assert.That(configuration.EnableLocalContentInEditor, Is.False, "Without a saved preference Local Mode is off, whatever the shared asset says.");
				Assert.That(EditorPrefs.HasKey(key), Is.False, "Reading must not save a preference.");

				var before = File.ReadAllBytes(path);
				configuration.EnableLocalContentInEditor = true;
				var parameters = configuration.ParameterProvider;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				configuration = AssetDatabase.LoadAssetAtPath<ContentConfiguration>(path);
				Assert.That(configuration.EnableLocalContentInEditor, Is.True);
				Assert.That(EditorPrefs.GetBool(key), Is.True);
				Assert.That(File.ReadAllBytes(path), Is.EqualTo(before), "The preference is never written to the shared asset.");

				configuration.EnableLocalContentInEditor = false;
				File.WriteAllText(path, WithFormerField(true));
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				configuration = AssetDatabase.LoadAssetAtPath<ContentConfiguration>(path);
				Assert.That(configuration.EnableLocalContentInEditor, Is.False, "A teammate's asset change cannot opt this developer in.");
				Assert.That(parameters.EnableLocalContentInEditor, Is.True, "A captured runtime configuration must not change mid-session.");
				Assert.That(configuration.ParameterProvider.EnableLocalContentInEditor, Is.False);
			}
			finally
			{
				AssetDatabase.DeleteAsset(path);
				if (configuration != null) UnityEngine.Object.DestroyImmediate(configuration);
				if (hadValue) EditorPrefs.SetBool(key, oldValue); else EditorPrefs.DeleteKey(key);
				ResetCache();
			}
		}

		[Test]
		public void Preference_reads_are_cached_until_reset()
		{
			var path = "Assets/__LocalModePreference_" + Guid.NewGuid().ToString("N") + ".asset";
			var key = Key(Application.dataPath);
			var hadValue = EditorPrefs.HasKey(key);
			var oldValue = EditorPrefs.GetBool(key);
			var configuration = ScriptableObject.CreateInstance<ContentConfiguration>();
			try
			{
				AssetDatabase.CreateAsset(configuration, path);
				configuration.EnableLocalContentInEditor = true;
				EditorPrefs.SetBool(key, false);
				Assert.That(configuration.EnableLocalContentInEditor, Is.True, "Reads use the cached preference.");
				ResetCache();
				Assert.That(configuration.EnableLocalContentInEditor, Is.False);
			}
			finally
			{
				AssetDatabase.DeleteAsset(path);
				if (configuration != null) UnityEngine.Object.DestroyImmediate(configuration);
				if (hadValue) EditorPrefs.SetBool(key, oldValue); else EditorPrefs.DeleteKey(key);
				ResetCache();
			}
		}
	}
}
