#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
using Beamable.Common;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#if UNITY_2019_4_OR_NEWER
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditorInternal;
using UnityEngine.UIElements;
#endif

namespace Beamable.Editor.ToolbarExtender
{
	public static class BeamableToolbarExtender
	{

		private static BeamEditorContext _editorAPI;
		private static List<BeamableToolbarMenuItem> _assistantMenuItems;
		
		private static Action _repaint;

		public static void Repaint() => _repaint?.Invoke();

		public static void LoadToolbarExtender()
		{
			_repaint = () =>
			{
				BeamableToolbarCallbacks.m_toolbarType.GetMethod("RepaintToolbar", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
			};


			BeamableToolbarCallbacks.OnToolbarGUI = OnGUI;

			if (!BeamEditor.IsInitialized)
			{
				Debug.LogError("Beamable Toolbar cannot load because Beamable is not initialized. ");
				return;
			}

			var api = BeamEditorContext.Default;
			_editorAPI = api;

			// Load and inject Beamable Menu Items (necessary due to multiple package split of SDK) --- sort them by specified order, and alphabetically when tied.
			var menuItemsSearchInFolders = BeamEditor.CoreConfiguration.BeamableMenuItemsPath.Where(Directory.Exists).ToArray();
			var menuItemsGuids = BeamableAssetDatabase.FindAssets<BeamableToolbarMenuItem>(menuItemsSearchInFolders);
			_assistantMenuItems = menuItemsGuids.Select(guid => AssetDatabase.LoadAssetAtPath<BeamableToolbarMenuItem>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
			_assistantMenuItems.Sort((mi1, mi2) =>
			{
				var orderComp = mi1.Order.CompareTo(mi2.Order);
				var labelComp = string.Compare(mi1.RenderLabel(_editorAPI)?.text, mi2.RenderLabel(_editorAPI)?.text, StringComparison.Ordinal);
			
				return orderComp == 0 ? labelComp : orderComp;
			});

		}

		static void OnGUI(IMGUIContainer container)
		{
			if (_editorAPI == null) return;
			
			BeamGUI.LoadAllIcons();

			EditorGUILayout.BeginHorizontal(new GUIStyle
			{
				padding = new RectOffset(0, 0, 2, 0)
			});

			try
			{
				var badgeColor = new Color(0,0,0,.3f);
				if (_editorAPI.BeamCli.CurrentRealm?.IsProduction ?? false)
				{
					badgeColor = new Color(1, 0, 0, .5f);
				}
				else if (_editorAPI.BeamCli.CurrentRealm?.IsStaging ?? false)
				{
					badgeColor = new Color(1, .5f, 0, .5f);
				}

				var realmDisplay = _editorAPI.BeamCli.CurrentRealm?.DisplayName ?? "<no realm>";
				var versionDisplay = _editorAPI.BeamCli.PackageVersion.ToString();
				if (_editorAPI.BeamCli.PackageVersion.IsNightly)
				{
					versionDisplay = "nightly";
				}
				var titleContent = new GUIContent(realmDisplay + " (" + versionDisplay + ")");

				var didClick = GUILayout.Button(titleContent, new GUIStyle(EditorStyles.toolbarButton)
				{
					alignment = TextAnchor.MiddleLeft,
					padding = new RectOffset(24, 0, 0, 0),
					fixedHeight = EditorStyles.toolbarButton.fixedHeight - 4,
				});
				var buttonRect = GUILayoutUtility.GetLastRect();
				if (didClick)
				{
					// create the menu and add items to it
					var menu = new GenericMenu();

					_assistantMenuItems.ForEach(item => item.ContextualizeMenu(_editorAPI, menu));
					_assistantMenuItems
						.ForEach(item =>
						{
							var label = item.RenderLabel(_editorAPI);
							if (label == null) return;

							menu.AddItem(label, false, data => item.OnItemClicked((BeamEditorContext) data),
							             _editorAPI);
						});

					menu.ShowAsContext();
				}

				var badgeWidth = 4;
				var badgeRect = new Rect(buttonRect.x, buttonRect.y-1, badgeWidth, buttonRect.height+2);
				
				var iconRect = new Rect(buttonRect.x+3, buttonRect.y-1, 20, 18);
				GUI.DrawTexture(iconRect, BeamGUI.iconBeamableSmall);
				EditorGUI.DrawRect(badgeRect, badgeColor);
			}
			finally
			{
				EditorGUILayout.EndHorizontal();
				_repaint();
			}

		}
	}
}
#endif
