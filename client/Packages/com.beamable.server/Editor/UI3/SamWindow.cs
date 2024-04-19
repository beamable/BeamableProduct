using Beamable.Common;
using Beamable.Editor.Microservice.UI3.Components.SamCardVisualElement;
using Beamable.Editor.UI;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI3
{
	[Serializable]
	public class SamWindow : BeamEditorWindow<SamWindow>
	{
		public CodeService _codeService;
		
		
		public VisualElement _windowRoot;
		
		public override bool ShowLoading => false;

		static SamWindow()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Sam Editor",
				FocusOnShow = false,
				DockPreferenceTypeName = inspector.AssemblyQualifiedName,
				RequireLoggedUser = false,
			};
		}

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Sam Editor %g",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		protected override void Build()
		{
			// nothing to do.
			
			
		}

		protected override async Promise BuildAsync()
		{
			
			var root = this.GetRootVisualContainer();
			root.Clear();

			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI_3}/SamWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI_3}/SamWindow.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();
			_windowRoot.userData = ActiveContext.ServiceScope;

			root.Add(_windowRoot);
			
			
			_codeService = Scope.GetService<CodeService>();
			
			Scope.GetService<BeamableDispatcher>().Run("sam-window-evt-loop", Loop());
			
			BindData();
			
			// mark everything as disabled while we load and verify data is still correct :(
			root.EnableInClassList("disabled", true);
			root.Query<Button>().ForEach(x => x.SetEnabled(false));

			var dbg = _windowRoot.Q<VisualElement>(name: "debug");
			dbg.Add(new Label("loading"));
			
			await _codeService.OnReady;

			var model = Scope.GetService<SamModel>();
			model.Refresh(_codeService);
			
			root.EnableInClassList("disabled", false);
			BindData();
			
		}

		void BindData()
		{
			var model = Scope.GetService<SamModel>();
			var dbg = _windowRoot.Q<VisualElement>(name: "debug");
			var cards = _windowRoot.Q<VisualElement>(name: "cards");
			cards.Clear();
			dbg.Clear();
			foreach (var service in model.services)
			{
				var cardElement = new SamCardVisualElement(service.name);
				cards.Add(cardElement);
				cardElement.Refresh();
			}
		}

		public int lastVersion = -1;
		IEnumerator Loop()
		{
			// var delay = new WaitForSecondsRealtime()
			while (true)
			{
				var model = Scope.GetService<SamModel>();

				if (lastVersion != model.version)
				{
					// Debug.Log("SOMETHING HAPPENED!");
					// Debug.Log(JsonUtility.ToJson(model));
					BindData();
				}

				lastVersion = model.version;
				yield return null;
			}
		}
		
	}
}
