using Beamable.Common;
using Beamable.Editor.UI;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow2 : BeamEditorWindow<UsamWindow2>, IDelayedActionWindow
	{
		public UsamService usam;
		private List<Action> _actions = new List<Action>();

		public static Texture iconService;
		public static Texture iconStorage;
		public static Texture iconOpenApi;
		public static Texture iconOpenMongoExpress;
		public static Texture iconSettings;
		public static Texture iconOpenProject;
		public static Texture iconMoreOptions;
		public static Texture iconPlay;
		
		public CardButton selectedCard;

		
		static UsamWindow2()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Beam Services",
				FocusOnShow = false,
				DockPreferenceTypeName = inspector.AssemblyQualifiedName,
				RequireLoggedUser = true,
			};
		}
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Beam Services %q",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		
		protected override void Build()
		{
			usam = ActiveContext.ServiceScope.GetService<UsamService>();
			usam.Reload();
		}

		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private void OnGUI()
		{
			LoadIcons();
			
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

			if (usam == null)
			{
				EditorGUILayout.SelectableLabel("Waiting for data...");
				return;
			}

			PrepareCards();
			selectedCard = cards.FirstOrDefault(x => x.name == selectedBeamoId);

			DrawMain();

			{ // perform delayed actions
				foreach (var act in _actions)
				{
					act?.Invoke();
				}

				_actions.Clear();
			}
		}


		void LoadIcons()
		{
			if (iconService == null)
			{
				iconService =
					EditorResources.Load<Texture>(
						"Packages/com.beamable.server/Editor/UI/Icons/MS not running without wifi.png", true);
			}
			
			if (iconStorage == null)
			{
				iconStorage =
					EditorResources.Load<Texture>("Packages/com.beamable.server/Editor/UI/Icons/SO running.png", true);
			}
			
			
			
			if (iconSettings == null)
			{
				iconSettings = EditorGUIUtility.IconContent("Settings").image;
			}


			if (iconOpenApi == null)
			{
				iconOpenApi = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
			}

			if (iconOpenMongoExpress == null)
			{
				iconOpenMongoExpress =
					EditorResources.Load<Texture>("Packages/com.beamable.server/Editor/UI/Icons/Database_light.png", true);
			}

			if (iconOpenProject == null)
			{
				iconOpenProject = EditorGUIUtility.FindTexture("cs Script Icon");
			}
			if (iconMoreOptions == null)
			{
				iconMoreOptions = EditorGUIUtility.IconContent("pane options@2x").image;
			}
			if (iconPlay == null)
			{
				iconPlay = EditorGUIUtility.FindTexture("PlayButton");
			}
		}

		void DrawNoContextGui()
		{
			EditorGUILayout.SelectableLabel("No Beamable context is available");
		}
		
		void DrawWaitingForContextGui()
		{
			EditorGUILayout.SelectableLabel("Loading Beamable...");
		}

		void DrawNotLoggedInGui()
		{
			EditorGUILayout.SelectableLabel("Must log into Beamable...");
		}

		public void AddDelayedAction(Action act)
		{
			_actions.Add(act);
		}
	}
}
