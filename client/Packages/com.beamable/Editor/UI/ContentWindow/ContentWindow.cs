using Beamable;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using Editor.ContentService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow : BeamEditorWindow<ContentWindow>
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
			"Content Manager",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async Task Init() => _ = await GetFullyInitializedWindow();

		protected override void Build()
		{
			if (_contentService == null)
			{
				_contentService = Scope.GetService<CliContentService>();
				_contentService.OnManifestUpdated += Build;
				_ = _contentService.Reload().Then(_ =>
				{
					RegisterForOnManifestUpdated();
					SetEditorSelection();
				});
			}
			else
			{
				RegisterForOnManifestUpdated();
			}
			

			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			
			_contentConfiguration = ContentConfiguration.Instance;
			
			BuildHeaderFilters();
			
			BuildContentTypeHierarchy();

			BuildHeaderStyles();
			
			BuildContentStyles();
			
			BuildItemsPanelStyles();
			
			Repaint();

			void RegisterForOnManifestUpdated()
			{
				_contentService.OnManifestUpdated -= SetEditorSelection;
				_contentService.OnManifestUpdated += SetEditorSelection;
			}
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
				BeamGUI.DrawVerticalSeparatorLine(new RectOffset(MARGIN_SEPARATOR_WIDTH, MARGIN_SEPARATOR_WIDTH, 15, 15), Color.gray);
				DrawContentItemPanel();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
		}
		
		private List<LocalContentManifestEntry> GetCachedManifestEntries()
		{
			var localContentManifestEntries = new List<LocalContentManifestEntry>();
			if (_contentService != null)
			{
				localContentManifestEntries.AddRange(_contentService.CachedManifest.Values);
			}
			return localContentManifestEntries;
		}
		
	}
}
