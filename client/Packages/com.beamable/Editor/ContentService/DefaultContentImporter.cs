using Beamable.Common;
using Beamable.Common.Content;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
	/// <summary>
	/// Opt-in, user-initiated seeding of Beamable's default content (currency gems/coins) into a
	/// realm that has never had content published.
	///
	/// This replaces the old behaviour where <see cref="CliContentService.Reload"/> silently seeded
	/// and published default content at editor init time. That ran AssetDatabase / Addressables
	/// mutation during initialization, which was risky (potential refresh loops). Seeding now only
	/// happens when the user explicitly asks for it — via the Content Manager empty-state prompt or
	/// the "Import Default Content" menu item.
	///
	/// The currency icons are Addressable sprite references. The seed sprites ship in the
	/// Unity-ignored <c>DefaultAssets~</c> folder with pinned-GUID meta files; importing copies them
	/// into the project (where the pinned GUID registers for the first time, avoiding a collision),
	/// marks them addressable, and only then seeds the content — whose serialized references already
	/// point at the pinned GUIDs.
	/// </summary>
	public static class DefaultContentImporter
	{
		private const string DISMISS_PREF_PREFIX = "beamable.contentmanager.defaultcontent.dismissed";

		/// <summary>Per-realm EditorPrefs key controlling whether the in-window prompt is shown.</summary>
		public static string DismissKey(string cid, string pid) => $"{DISMISS_PREF_PREFIX}.{cid}.{pid}";

		public static bool IsDismissed(string cid, string pid)
		{
			if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
				return false;
			return EditorPrefs.GetBool(DismissKey(cid, pid), false);
		}

		public static void SetDismissed(string cid, string pid, bool dismissed = true)
		{
			if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
				return;
			EditorPrefs.SetBool(DismissKey(cid, pid), dismissed);
		}

		[MenuItem(Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Import Default Content")]
		public static async void ImportDefaultContent_MenuItem()
		{
			bool confirmed = EditorUtility.DisplayDialog(
				"Import Default Content",
				"This will create Beamable's default content (gems & coins currencies) as local content, " +
				"copying their icon sprites into Assets/Beamable/DefaultAssets and registering them as " +
				"Addressables.",
				"Import",
				"Cancel");

			if (confirmed)
			{
				await ImportDefaultContent();
			}
		}

		/// <summary>
		/// Shared import routine used by both the menu item and the Content Manager prompt. Seeds the
		/// default content as local content; the user can review and publish it themselves.
		/// </summary>
		public static async Task ImportDefaultContent()
		{
			var api = BeamEditorContext.Default;
			await api.InitializePromise;
			var contentService = api.ServiceScope.GetService<CliContentService>();

			// 1. Ensure Addressables settings exist (GetSettings(true) creates them if missing).
			var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
			if (settings == null)
			{
				EditorUtility.DisplayDialog(
					"Addressables Required",
					"Beamable's default content uses Unity Addressables for its currency icons, but the " +
					"Addressables settings could not be created. Please configure the Addressables package " +
					"and try again.",
					"OK");
				return;
			}

			// 2. Copy seed sprites into the project and register them as addressables (idempotent).
			if (!EnsureDefaultAssetsImported(settings))
			{
				return;
			}

			// 3. Seed the content locally. The default content's serialized icon references already
			//    point at the pinned GUIDs, so no per-asset remap is needed.
			SeedDefaultContent(contentService);

			// 4. Refresh the Content Manager view. The seeded content is left local for the user to
			//    review and publish on their own terms.
			await contentService.Reload();
		}

		/// <summary>
		/// Copies the seed sprites out of the Unity-ignored source folder into the project and
		/// ensures each is an entry in the "Beamable Assets" Addressables group. Idempotent: sprites
		/// already present (by pinned GUID) are reused rather than re-copied.
		/// </summary>
		private static bool EnsureDefaultAssetsImported(AddressableAssetSettings settings)
		{
			var sourceRoot = ResolveSourceRoot();
			if (string.IsNullOrEmpty(sourceRoot) || !Directory.Exists(sourceRoot))
			{
				EditorUtility.DisplayDialog(
					"Default Content Unavailable",
					$"Could not locate the default content seed assets at '{Constants.Directories.DEFAULT_ASSET_SOURCE_DIR}'.",
					"OK");
				return false;
			}

			var knownGuids = new List<string>();
			var copiedAnything = false;

			foreach (var typeDir in Directory.GetDirectories(sourceRoot))
			{
				var typeName = Path.GetFileName(typeDir); // e.g. "currency"
				var destDir = $"{Constants.Directories.ASSET_DIR}/{typeName}";

				foreach (var src in Directory.GetFiles(typeDir))
				{
					if (src.EndsWith(".meta"))
						continue;

					var metaSrc = src + ".meta";
					if (!File.Exists(metaSrc))
						continue; // a pinned-GUID meta is required

					var knownGuid = ReadGuidFromMeta(metaSrc);
					if (string.IsNullOrEmpty(knownGuid))
						continue;

					knownGuids.Add(knownGuid);

					// Short-circuit: already imported (by pinned GUID) -> reuse, don't duplicate.
					var existingPath = AssetDatabase.GUIDToAssetPath(knownGuid);
					if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath))
						continue;

					Directory.CreateDirectory(destDir);
					var fileName = Path.GetFileName(src);
					var dest = Path.Combine(destDir, fileName);
					File.Copy(src, dest, true);
					// Copy the pinned-GUID meta so the imported asset keeps the known GUID.
					File.Copy(metaSrc, dest + ".meta", true);
					copiedAnything = true;
				}
			}

			if (copiedAnything)
			{
				AssetDatabase.Refresh();
			}

			// Ensure the addressables group exists, then ensure each seed sprite is an entry in it.
			var group = settings.FindGroup(Constants.BEAMABLE_ASSET_GROUP);
			if (group == null)
			{
				group = settings.CreateGroup(
					Constants.BEAMABLE_ASSET_GROUP,
					setAsDefaultGroup: false,
					readOnly: false,
					postEvent: true,
					schemasToCopy: new List<AddressableAssetGroupSchema>(),
					typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
			}

			var entries = new List<AddressableAssetEntry>();
			foreach (var knownGuid in knownGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(knownGuid);
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogWarning($"[Default Content] Seed sprite with GUID {knownGuid} did not import; its currency icon may not resolve.");
					continue;
				}

				var entry = settings.CreateOrMoveEntry(knownGuid, group);
				if (entry != null)
					entries.Add(entry);
			}

			if (entries.Count > 0)
			{
				settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true);
			}

			return true;
		}

		/// <summary>
		/// Loads the default content objects shipped in the package and saves them as local content.
		/// </summary>
		private static void SeedDefaultContent(CliContentService contentService)
		{
			string[] guids = BeamableAssetDatabase.FindAssets<ContentObject>(new[] { Constants.Directories.DEFAULT_DATA_DIR });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				ContentObject obj = AssetDatabase.LoadAssetAtPath<ContentObject>(path);
				if (obj == null)
					continue;

				string fileName = Path.GetFileNameWithoutExtension(path);
				obj.SetContentName(fileName);
				contentService.SaveContent(obj);
			}
		}

		/// <summary>
		/// Resolves the seed-asset source folder to an absolute path. The folder is "~"-suffixed so
		/// Unity ignores it; we read it with System.IO. <see cref="Path.GetFullPath"/> resolves the
		/// "Packages/com.beamable/..." virtual path for embedded/local packages.
		/// </summary>
		private static string ResolveSourceRoot()
		{
			var relative = Constants.Directories.DEFAULT_ASSET_SOURCE_DIR;
			if (Directory.Exists(relative))
				return relative;

			var full = Path.GetFullPath(relative);
			return Directory.Exists(full) ? full : null;
		}

		private static string ReadGuidFromMeta(string metaPath)
		{
			foreach (var line in File.ReadLines(metaPath))
			{
				var trimmed = line.Trim();
				if (trimmed.StartsWith("guid:"))
					return trimmed.Substring("guid:".Length).Trim();
			}

			return null;
		}
	}
}
