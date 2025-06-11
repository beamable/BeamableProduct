using Beamable;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using Editor.CliContentManager;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow : BeamEditorWindow<UI.ContentWindow.ContentWindow>
	{
		private const int MARGIN_SEPARATOR_WIDTH = 10;
		
		private UnityEditor.Editor nestedEditor;
		
		[SerializeField]
		private SearchData _contentSearchData;
		private ContentTypeReflectionCache _contentTypeReflectionCache;
		private CliContentService _contentService;
		private ContentConfiguration _contentConfiguration;
		private Vector2 _horizontalScrollPosition;

		static ContentWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "New Content Manager",
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = true,
				RequirePid = true,
			};
		}
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"New Content Manager",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		protected override void Build()
		{
			
			_contentService = ActiveContext.ServiceScope.GetService<CliContentService>();
			
			_contentService.Reload();
			
			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			
			_contentConfiguration = ContentConfiguration.Instance;
			
			BuildHeaderFilters();
			
			BuildContentTypeHierarchy();

			BuildHeaderStyles();
			
			BuildContentStyles();
			
			BuildItemsPanelStyles();
		}

		protected override void DrawGUI()
		{
			DrawHeader();

			DrawContentData();
			
			RunDelayedActions();
		}

		private void DrawContentData()
		{
			_horizontalScrollPosition = EditorGUILayout.BeginScrollView(_horizontalScrollPosition);
			EditorGUILayout.BeginHorizontal();
			{
				DrawContentGroupPanel();
				BeamGUI.DrawVerticalSeparatorLine(new RectOffset(MARGIN_SEPARATOR_WIDTH, MARGIN_SEPARATOR_WIDTH, 15, 15));
				DrawContentItemPanel();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
		}
	}
}
