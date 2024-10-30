using Beamable.Common;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Library
{
	public partial class BeamLibraryWindow : BeamEditorWindow<BeamLibraryWindow>
	{
		
		static BeamLibraryWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Beam Library",
				FocusOnShow = false,
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				RequireLoggedUser = true,
			};
		}
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Beam Library",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();


		public LibraryService library;
		
		
		protected override void Build()
		{
			library = ActiveContext.ServiceScope.GetService<LibraryService>();
			library.Reload();
		}
		
		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private void OnGUI()
		{
			BeamGUI.LoadAllIcons();
			titleContent = new GUIContent("Library", BeamGUI.iconBeamableSmall);
			var ctx = ActiveContext;
			if (ctx == null)
			{
				DrawNoContextGui();
				return;
			}
			
			if (!ctx.InitializePromise.IsCompleted)
			{
				DrawWaitingForContextGui();
				return;
			}

			if (!ctx.IsAuthenticated)
			{
				DrawNotLoggedInGui();
				return;
			}

			if (library == null)
			{
				EditorGUILayout.LabelField("No library available.");
				return;
			}
			
			DrawMain();
			
			RunDelayedActions();
		}
	}
}
