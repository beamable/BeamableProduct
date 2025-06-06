using Beamable;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Editor.CliContentManager;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Editor.UI2.ContentWindow
{
	public partial class ContentWindow : BeamEditorWindow<ContentWindow>
	{
		private const int MARGIN_SEPARATOR_WIDTH = 10;
		
		[SerializeField]
		private SearchData _contentSearchData;
		private ContentTypeReflectionCache _contentTypeReflectionCache;
		private CliContentService _contentService;
		private ContentConfiguration _contentConfiguration;

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

			BuildItemsHierarchy();
			
			BuildHeaderStyles();
			
			BuildContentStyles();
		}

		protected override void DrawGUI()
		{
			DrawHeader();

			DrawContentData();
			
			RunDelayedActions();
		}

		private void DrawContentData()
		{
			EditorGUILayout.BeginHorizontal();
			{
				DrawContentGroupPanel();
				DrawVerticalLineSeparator(new RectOffset(MARGIN_SEPARATOR_WIDTH, MARGIN_SEPARATOR_WIDTH, 15, 15));
				DrawContentItemPanel();
			}
			EditorGUILayout.EndHorizontal();
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
