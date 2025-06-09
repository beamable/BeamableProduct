using Beamable;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
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
				DrawVerticalLineSeparator(new RectOffset(MARGIN_SEPARATOR_WIDTH, MARGIN_SEPARATOR_WIDTH, 15, 15));
				DrawContentItemPanel();
				DrawVerticalLineSeparator(new RectOffset(MARGIN_SEPARATOR_WIDTH, MARGIN_SEPARATOR_WIDTH, 15, 15));
				DrawContentInspector();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
		}

		private static void DrawVerticalLineSeparator(RectOffset margin = null)
		{
			var verticalLineStyle = new GUIStyle
			{
				normal = {background = EditorGUIUtility.whiteTexture},
				fixedWidth = 1,
				margin = margin ?? new RectOffset(0, 0, 0, 0)
			};
			GUILayout.Box(GUIContent.none, verticalLineStyle,
			              GUILayout.ExpandHeight(true),
			              GUILayout.Width(1f));
		}
		
		private static void DrawHorizontalLineSeparator(RectOffset margin = null)
		{
			var horizontalLineStyle = new GUIStyle
			{
				normal = {background = EditorGUIUtility.whiteTexture},
				fixedHeight = 1,
				margin = margin ?? new RectOffset(0, 0, 0, 0)
			};
			GUILayout.Box(GUIContent.none, horizontalLineStyle,
			              GUILayout.ExpandWidth(true),
			              GUILayout.Height(1f));
		}
		
		private Texture2D CreateColorTexture(Color color)
		{
			var texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, color);
			texture.Apply();
			return texture;
		}
	}
}
