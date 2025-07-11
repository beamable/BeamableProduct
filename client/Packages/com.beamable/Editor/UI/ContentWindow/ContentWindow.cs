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
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow : BeamEditorWindow<ContentWindow>
	{
		private const int MARGIN_SEPARATOR_WIDTH = 10;

		private ContentWindowStatus _windowStatus = ContentWindowStatus.Normal;
		
		[SerializeField]
		private SearchData _contentSearchData;
		private ContentTypeReflectionCache _contentTypeReflectionCache;
		private CliContentService _contentService;
		private ContentConfiguration _contentConfiguration;
		private Vector2 _horizontalScrollPosition;
		private int _lastManifestChangedCount;

		static ContentWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Content Manager",
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
			if(_windowStatus == ContentWindowStatus.Building && _contentService != null)
				return;
			
			if (_contentService == null)
			{
				ChangeWindowStatus(ContentWindowStatus.Building, false);
				_contentService = Scope.GetService<CliContentService>();
				_ = _contentService.Reload().Then(_ =>
				{
					ChangeWindowStatus(ContentWindowStatus.Normal, false);
					Build();
				});
				return;
			}
			
			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			
			_contentConfiguration = ContentConfiguration.Instance;
			
			BuildHeaderFilters();
			
			BuildContentTypeHierarchy();

			BuildHeaderStyles();
			
			BuildContentStyles();
			
			BuildItemsPanelStyles();

			ClearCaches();
			
			Repaint();
		}

		private void ReloadData()
		{
			ClearCaches();
			_allTags = _contentService.TagsCache;
			SetEditorSelection();
			
			if(!_contentService.HasChangedContents)
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
			}
		}

		private void ClearCaches()
		{
			_filteredCache.Clear();
			_sortedCache.Clear();
		}

		protected override void DrawGUI()
		{
			if (_contentService == null)
			{
				Build();
				return;
			}

			if (_windowStatus == ContentWindowStatus.Building)
			{
				DrawBlockLoading("Loading Contents...");
				return;
			}

			if (_contentService.ManifestChangedCount != _lastManifestChangedCount)
			{
				_lastManifestChangedCount = _contentService.ManifestChangedCount;
				ReloadData();
			}
			
			DrawHeader();
			GUILayout.Space(1);
			switch (_windowStatus)
			{
				case ContentWindowStatus.Normal:
					DrawContentData();
					break;
				case ContentWindowStatus.Publish:
					DrawPublishPanel();
					break;
				case ContentWindowStatus.Revert:
					DrawRevertPanel();
					break;
				case ContentWindowStatus.Validate:
					DrawValidatePanel();
					break;
			}
			RunDelayedActions();
		}

		private void DrawContentData()
		{
			EditorGUILayout.BeginVertical();
			_horizontalScrollPosition = EditorGUILayout.BeginScrollView(_horizontalScrollPosition);
			EditorGUILayout.BeginHorizontal();
			{
				DrawContentGroupPanel();
				BeamGUI.DrawVerticalSeparatorLine(new RectOffset(MARGIN_SEPARATOR_WIDTH, MARGIN_SEPARATOR_WIDTH, 15, 15), Color.gray);
				DrawContentItemPanel();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}
		
		private List<LocalContentManifestEntry> GetCachedManifestEntries()
		{
			var localContentManifestEntries = new List<LocalContentManifestEntry>();
			if (_contentService != null)
			{
				localContentManifestEntries.AddRange(_contentService.EntriesCache.Values);
			}
			return localContentManifestEntries;
		}
		
	}

	public enum ContentWindowStatus
	{
		Normal,
		Publish,
		Building,
		Revert,
		Validate
	}
}
