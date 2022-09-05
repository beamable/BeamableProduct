using Beamable.Common;
using Beamable.Common.Api.CloudData;
using Beamable.Editor.Toolbox.UI;
using Beamable.Editor.UI;
using Beamable.Editor.UI.CloudDataWindow.Models;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants;

namespace Beamable.Editor.UI.CloudDataWindow
{
	public class CloudDataWindow : BeamEditorWindow<CloudDataWindow>
	{
		private VisualElement _windowRoot;
		private CloudDataViewService _model;
		static CloudDataWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Cloud Data Window",
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = true,
			};
		}

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Commons.OPEN + " " +
			"Cloud Data Window",
			priority = MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_1
		)]
		public static async void Init() => await GetFullyInitializedWindow();
		public static async void Init(BeamEditorWindowInitConfig initParameters) => await GetFullyInitializedWindow(initParameters);
	
		protected override void Build()
		{

			// Refresh if/when the user logs-in or logs-out while this window is open
			ActiveContext.OnUserChange += _ => BuildWithContext();

			_model = new CloudDataViewService();
			_model.OnAvailableMetaDataDownloaded += HandleMetaDataDownloaded;
			// _model = ActiveContext.ServiceScope.GetService<IToolboxViewService>();
			//
			// // Force refresh to build the initial window
			// _model?.Destroy();
			//
			// _model.UseDefaultWidgetSource();
			// _model.Initialize();

			SetForContent();
			_model.Init();
		}

		private void HandleMetaDataDownloaded(Dictionary<CloudMetaData, string> cloudMetaDatas)
		{
			var s = _windowRoot.Q("window-main");
			s.Clear();
			foreach (KeyValuePair<CloudMetaData, string> metaData in cloudMetaDatas)
			{
				var label = new Label($"<b>{metaData.Key.@ref}</b> <size=15>v{metaData.Key.version}</size>");
				label.AddToClassList("refName");
				var content = new Label(metaData.Value);
				content.AddToClassList("content");
				
				s.Add(label);
				s.Add(content);
			}
		}

		private void SetForContent()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();
			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Directories.BEAMABLE_PACKAGE_EDITOR_UI}/{nameof(CloudDataWindow)}/{nameof(CloudDataWindow)}.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{Directories.BEAMABLE_PACKAGE_EDITOR_UI}/{nameof(CloudDataWindow)}/{nameof(CloudDataWindow)}.uss");
			_windowRoot.name = nameof(_windowRoot);
			root.Add(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();
		}
	}
}
