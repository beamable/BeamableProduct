using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using static Beamable.Common.Constants.MenuItems.Windows;

namespace Beamable.Assets.Editor
{
	public class CheckMissingReferencesWindow : EditorWindow
	{
		static string findString;
		static string replaceString;
		public static int missingCount = -1;

		[MenuItem(
			Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/" +
			"Check Missing References",
			priority = Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
		public static void FindMissingScripts()
		{
			EditorWindow.GetWindow(typeof(CheckMissingReferencesWindow));
		}


		void OnGUI()
		{

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.LabelField("Missing Scripts:");
				EditorGUILayout.LabelField("" + (missingCount == -1 ? "---" : missingCount.ToString()));
			}
			EditorGUILayout.EndHorizontal();
			findString = EditorGUILayout.TextField("Find String:", findString);
			replaceString = EditorGUILayout.TextField("Replace string: ", replaceString);
			EditorGUILayout.LabelField("Where would you like to search for missing scripts?");
			if (GUILayout.Button("Fix Known References"))
			{
				CheckMissingReferences.FixKnownReferences();
			}

			if (GUILayout.Button("Packages"))
			{

				CheckMissingReferences.FindMissing(Path.Combine("Packages", "com.beamable"), findString, replaceString);
			}

			if (GUILayout.Button("Assets"))
			{
				CheckMissingReferences.FindMissing(Application.dataPath, findString, replaceString);
			}
		}
	}

	public class CheckMissingReferences
	{
		private static HashSet<string> output = new HashSet<string>();

		public static void CheckMissingReferencesForBuild()
		{
			FindMissing(Path.Combine("Packages", "com.beamable"));
			if (output.Count > 0)
			{
				foreach (string prefab in output)
				{
					Debug.LogError(prefab);
				}

				throw new Exception("Prefabs are missing script references.");
			}
		}

		public static void FixKnownReferences()
		{
			var packageDir = Path.Combine("Packages", "com.beamable");
			foreach (var known in KnownMissingReferences)
			{
				Debug.LogWarning($"Replacing 2019 versions of {known.Name}");
				FindMissing(packageDir, known.Reference2019, known.Reference2018);
			}
		}

		public static void FindMissing(string dirs, string findString = null, string replaceString = null)
		{
			CheckMissingReferencesWindow.missingCount = 0;


			string[] files = System.IO.Directory.GetFiles(dirs, "*.prefab", System.IO.SearchOption.AllDirectories);
			if (!UnityEditorInternal.InternalEditorUtility.inBatchMode)
			{
				EditorUtility.DisplayProgressBar("Searching Prefabs", "", 0.0f);
				EditorUtility.DisplayCancelableProgressBar("Searching Prefabs", "Found " + files.Length + " prefabs",
				                                           0.0f);

				Scene currentScene = EditorSceneManager.GetActiveScene();
				string scenePath = currentScene.path;
				EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
			}

			for (int i = 0; i < files.Length; i++)
			{
				string prefabPath = files[i].Replace(Application.dataPath, "Assets");
				if (!UnityEditorInternal.InternalEditorUtility.inBatchMode)
				{
					if (EditorUtility.DisplayCancelableProgressBar("Processing Prefabs " + i + "/" + files.Length,
					                                               prefabPath, (float)i / (float)files.Length))
						break;
				}

				var newFile = Path.Combine(Path.Combine(Application.dataPath, ".."), files[i]);


				if (findString != "" && findString != null)
				{
					string text = File.ReadAllText(newFile);
					if (text.Contains(findString))
					{
						text = text.Replace(findString, replaceString);
						File.SetAttributes(newFile, FileAttributes.Normal);
						File.WriteAllText(newFile, text);
						File.SetAttributes(newFile, FileAttributes.ReadOnly);
						AssetDatabase.Refresh();
					}
				}

				GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;

				if (go != null)
				{
					FindInGO(go);
					go = null;
					EditorUtility.UnloadUnusedAssetsImmediate(true);
				}
			}

			EditorUtility.UnloadUnusedAssetsImmediate(true);
			GC.Collect();
			EditorUtility.ClearProgressBar();

		}

		private static void FindInGO(GameObject go)
		{
			Component[] components = go.GetComponents<Component>();
			for (int i = 0; i < components.Length; i++)
			{
				if (components[i] == null)
				{
					CheckMissingReferencesWindow.missingCount++;
					Transform t = go.transform;

					string componentPath = go.name;
					while (t.parent != null)
					{
						componentPath = t.parent.name + "/" + componentPath;
						t = t.parent;
					}

					output.Add(componentPath);
					Debug.LogWarning("Prefab " + go.name + " has an empty script attached:\n" + componentPath, go);
				}

			}

			foreach (Transform child in go.transform)
			{
				FindInGO(child.gameObject);
			}
		}

		public static readonly List<MissingReferenceReplacement> KnownMissingReferences =
			new List<MissingReferenceReplacement> {
				MissingReferenceReplacement.Create(
					"BeamableShop",
					"{fileID: 11500000, guid: 1ced2f65d302a45b3813637ef42625cb, type: 3}",
					"{fileID: 11500000, guid: 33910d4313f0640f5bbd254e56a56d51, type: 3}"
				),
				MissingReferenceReplacement.Create(
					"BeamableCurrencyHUD",
					"{fileID: 11500000, guid: 47b9447b2c43b4fcea3455533163aecd, type: 3}",
					"{fileID: 11500000, guid: 92a13cf223a42436fae749bc16f51240, type: 3}"
				),
				MissingReferenceReplacement.Create(
					"BeamableModule",
					"{fileID: 11500000, guid: 69c56140d38ea4c468e687edddde0a69, type: 3}",
					"{fileID: 11500000, guid: cd0aab69f436a4675af3a3c2386bc32f, type: 3}"
				),

				MissingReferenceReplacement.Create(
					"BeamableConsole",
					"{fileID: 11500000, guid: c9c88ad96ceae4e548bf79692c03367a, type: 3}",
					"{fileID: 11500000, guid: 676ca6f5a246e4023853891f1a23e25c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Content Size Fitter",
					"{fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}",
					"{fileID: 1741964061, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Canvas Scaler",
					"{fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}",
					"{fileID: 1980459831, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Vertical Layout Group",
					"{fileID: 11500000, guid: 59f8146938fff824cb5fd77236b75775, type: 3}",
					"{fileID: 1297475563, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Horizontal Layout Group",
					"{fileID: 11500000, guid: 30649d3a9faa99c48a7b1166b86bf2a0, type: 3}",
					"{fileID: -405508275, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Graphic Raycaster",
					"{fileID: 11500000, guid: dc42784cf147c0c48a680349fa168899, type: 3}",
					"{fileID: 1301386320, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Image",
					"{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}",
					"{fileID: -765806418, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Button",
					"{fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}",
					"{fileID: 1392445389, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Layout Element",
					"{fileID: 11500000, guid: 306cc8c2b49d7114eaa3623786fc2126, type: 3}",
					"{fileID: 1679637790, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"Mask",
					"{fileID: 11500000, guid: 31a19414c41e5ae4aae2af33fee712f6, type: 3}",
					"{fileID: -1200242548, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"ScrollRect",
					"{fileID: 11500000, guid: 1aa08ab6e0800fa44ae55d278d1423e3, type: 3}",
					"{fileID: 1367256648, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
				MissingReferenceReplacement.Create(
					"ScrollBar",
					"{fileID: 11500000, guid: 2a4db7a114972834c8e4117be1d82ba3, type: 3}",
					"{fileID: -2061169968, guid: f70555f144d8491a825f0804e09c671c, type: 3}"),
			};
	}

	public class MissingReferenceReplacement
	{
		public string Name, Reference2019, Reference2018;

		public static MissingReferenceReplacement Create(string name, string ref2019, string ref2018)
		{
			return new MissingReferenceReplacement {
				Name = name,
				Reference2018 = ref2018,
				Reference2019 = ref2019
			};
		}
	}
}
