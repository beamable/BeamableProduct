using System;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Util;
using Beamable.Editor.ContentService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.ThirdParty.Splitter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow : BeamEditorWindow<ContentWindow>
	{
		private const int MARGIN_SEPARATOR_WIDTH = 10;
		private const int INDENT_WIDTH = 15;

		private ContentWindowStatus _windowStatus = ContentWindowStatus.Normal;
		
		[SerializeField]
		private SearchData _contentSearchData;
		private ContentTypeReflectionCache _contentTypeReflectionCache;
		private CliContentService _contentService;
		private BeamCli.BeamCli _cli;
		private ContentConfiguration _contentConfiguration;
		private Vector2 _horizontalScrollPosition;
		private int _lastManifestChangedCount;
		private int _lastProgressUpdateVersion;
		private EditorGUISplitView _mainSplitter;
		private bool _importingDefaultContent;

		static ContentWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Beam Content",
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = true,
				RequirePid = true,
			};
		}
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Beam Content",
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
					if (_windowStatus == ContentWindowStatus.Building)
					{
						ChangeWindowStatus(ContentWindowStatus.Normal, false);
					}

					Build();
				});
				return;
			}
			
			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			
			_contentConfiguration = Scope.GetService<ContentConfiguration>();

			FindLegacyContent();
			
			BuildHeaderFilters();
			
			BuildContentTypeHierarchy();

			ClearCaches();
			
			Repaint();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnEditorUpdate()
		{
			// We can use this to force Unity to repaint the window even if it isn't focused, making the Content Window more smoother when renaming and changing contents in the Inspector
			if (_contentService != null && _contentService.ManifestChangedCount != _lastManifestChangedCount)
			{
				_lastManifestChangedCount = _contentService.ManifestChangedCount;
				ReloadData();
				Repaint(); 
			}

			if (_contentService != null && _contentService.ProgressUpdateVersion != _lastProgressUpdateVersion)
			{
				_lastProgressUpdateVersion = _contentService.ProgressUpdateVersion;
				Repaint();
			}
		}

		private void ReloadData()
		{
			ClearCaches();
			_allTags = _contentService.TagsCache;
			SetEditorSelection();
			
			if(!_contentService.HasChangedContents && _windowStatus != ContentWindowStatus.SnapshotManager)
			{
				ChangeWindowStatusDelayed(ContentWindowStatus.Normal);
			}
		}

		private void ClearCaches()
		{
			_filteredCache.Clear();
			_sortedCache.Clear();
		}

		void ClearRenderedItems()
		{
			if (_frameRenderedItems == null)
			{
				_frameRenderedItems = new List<LocalContentManifestEntry>();
			}
			else
			{
				_frameRenderedItems.Clear();
			}
		}
		
		protected override void DrawGUI()
		{
			BuildHeaderStyles();
			
			BuildMigrationStyles();
			
			BuildItemsPanelStyles();
			
			_cli = ActiveContext.BeamCli;
			ClearRenderedItems();
			
			if (_contentService == null)
			{
				DrawBlockLoading("Loading Content...");
				return;
			}
			
			if (_windowStatus == ContentWindowStatus.Building)
			{
				DrawBlockLoading("Loading Contents...");
				return;
			}
			
			if (_gatheringSnapshots)
			{
				DrawBlockLoading("Loading snapshots");
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
					DrawNestedContent(DrawPublishPanel);
					break;
				case ContentWindowStatus.Revert:
					DrawNestedContent(DrawRevertPanel);
					break;
				case ContentWindowStatus.Validate:
					DrawNestedContent(DrawValidatePanel);
					break;
				case ContentWindowStatus.SnapshotManager:
					DrawSnapshotManager();
					break;
			}
			
			RunDelayedActions();
		}

		private void DrawContentData()
		{
			if (NeedsMigration)
			{
				DrawNestedContent(DrawMigration);
				return;
			}

			if (_importingDefaultContent)
			{
				DrawBlockLoading("Importing default content...");
				return;
			}

			if (ShouldShowDefaultContentPrompt())
			{
				DrawDefaultContentPrompt();
				return;
			}



			EditorGUILayout.BeginVertical();
			_horizontalScrollPosition = EditorGUILayout.BeginScrollView(_horizontalScrollPosition);
			
			EditorGUILayout.BeginHorizontal();
			{
				if (_mainSplitter == null)
				{
					var windowWidth = this.position.width;
					var startingWidthOfTypes = CONTENT_GROUP_PANEL_WIDTH;
					var normalizedWidth = startingWidthOfTypes / windowWidth;

					_mainSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, normalizedWidth, 1f - normalizedWidth);

					// the first time the splitter gets created, the window needs to force redraw itself
					//  so that the splitter can size itself correctly. 
					EditorApplication.delayCall += Repaint;
				}

				if (_mainSplitter.cellCount < 2)
				{
					_mainSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, 2);
				}

				
				_mainSplitter.BeginSplitView();
				DrawContentGroupPanel();
				_mainSplitter.Split(this);
				DrawContentItemPanel();
				_mainSplitter.EndSplitView();

			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			
			var bottomRect = EditorGUILayout.GetControlRect( GUILayout.Height(30f));
			
			var bottomRectController = new EditorGUIRectController(bottomRect);

			var allRenames = _contentService.GetAllRenames();
			var renamedCreatedIds = new HashSet<string>(allRenames.Select(r => r.CreatedFullId));
			var renamedDeletedIds = new HashSet<string>(allRenames.Select(r => r.DeletedFullId));

			var createdContents = _contentService.GetAllContentFromStatus(ContentStatus.Created)
				.Where(e => !renamedCreatedIds.Contains(e.FullId)).ToList();
			if (createdContents.Count > 0)
			{
				DrawFooterButton($"{createdContents.Count}  created", BeamGUI.iconStatusAdded, ContentFilterStatus.Created);
				bottomRectController.ReserveWidth(BASE_PADDING);
			}

			var modifiedContents = _contentService.GetAllContentFromStatus(ContentStatus.Modified);
			int modifiedCount = modifiedContents.Count + allRenames.Count;
			if (modifiedCount > 0)
			{
				DrawFooterButton($"{modifiedCount}  modified", BeamGUI.iconStatusModified, ContentFilterStatus.Modified);
				bottomRectController.ReserveWidth(BASE_PADDING);
			}

			var deletedContents = _contentService.GetAllContentFromStatus(ContentStatus.Deleted)
				.Where(e => !renamedDeletedIds.Contains(e.FullId)).ToList();
			if (deletedContents.Count > 0)
			{
				DrawFooterButton($"{deletedContents.Count}  deleted", BeamGUI.iconStatusDeleted, ContentFilterStatus.Deleted);
			}
			

			void DrawFooterButton(string buttonText, Texture buttonIcon, ContentFilterStatus statusEnum)
			{
				float lineSize = EditorGUIUtility.singleLineHeight;
				GUIStyle buttonStyle = new GUIStyle(EditorStyles.label)
				{
					fixedHeight = lineSize,
					alignment = TextAnchor.MiddleRight,
					padding = new RectOffset(BASE_PADDING, BASE_PADDING, 0,0)
				};
				
				var btnContent = new GUIContent(buttonText);
				var btnSize = buttonStyle.CalcSize(btnContent);
				Rect footerAreaRect = bottomRectController.ReserveWidth(btnSize.x + lineSize + BASE_PADDING * 3);
				var buttonRect = new Rect(footerAreaRect.x,  footerAreaRect.center.y - lineSize/2f, footerAreaRect.width, btnSize.y);
				
				var iconRect = new Rect(buttonRect.xMin + BASE_PADDING + lineSize/2f, buttonRect.center.y - lineSize/2f, lineSize, lineSize);
				GUI.DrawTexture(iconRect, buttonIcon, ScaleMode.ScaleToFit, true);
				EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
				if (GUI.Button(buttonRect, btnContent, buttonStyle))
				{
					_activeFilters.Clear();
					HashSet<string> statusFilter = GetFilterTypeActiveItems(ContentSearchFilterType.Status);
					string item = StatusMapToString[statusEnum];
					statusFilter.Add(item);
					UpdateActiveFilterSearchText();
				}
			}
			
			EditorGUILayout.EndVertical();
			
			
			
			{ // handle arrow-key support for selection
				var e = Event.current;
				var isDown = e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow;
				var isUp = e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow;

				var selection = MultiSelectItemIds;
				if (selection.Count > 0)
				{
					var currentIndex = _frameRenderedItems.FindIndex(c => c.FullId == selection.Last());
					if (e.shift)
					{
						if (currentIndex >= 0 && isUp)
						{
							AddEntryIdAsSelected(_frameRenderedItems[currentIndex - 1].FullId);
							e.Use();
							GUI.changed = true;
						} else if (currentIndex < _frameRenderedItems.Count - 1 && isDown)
						{
							AddEntryIdAsSelected(_frameRenderedItems[currentIndex + 1].FullId);
							e.Use();
							GUI.changed = true;
						}
					}
					else
					{
						if (currentIndex > 0 && isUp)
						{
							SetEntryIdAsSelected(_frameRenderedItems[currentIndex - 1].FullId);
							e.Use();
							GUI.changed = true;
						} else if (currentIndex < _frameRenderedItems.Count - 1 && isDown)
						{
							SetEntryIdAsSelected(_frameRenderedItems[currentIndex + 1].FullId);
							e.Use();
							GUI.changed = true;
						}
					}
				}
			}
		}
		
		/// <summary>
		/// Whether to offer the opt-in default-content import. Only shown when the realm's remote
		/// manifest is confirmed empty (not merely still loading or errored), there is no content
		/// at all yet, and the user hasn't dismissed the prompt for this realm.
		/// </summary>
		private bool ShouldShowDefaultContentPrompt()
		{
			if (_contentService == null)
				return false;
			if (!_contentService.RemoteManifestsLoaded || _contentService.RemoteManifestsErrored)
				return false;
			if (_contentService.RemoteManifestCount > 0)
				return false;
			if (_contentService.EntriesCache.Count > 0)
				return false;

			var cid = _cli?.CurrentRealm?.Cid;
			var pid = _cli?.CurrentRealm?.Pid;
			if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
				return false;

			return !DefaultContentImporter.IsDismissed(cid, pid);
		}

		private void DrawDefaultContentPrompt()
		{
			EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
			{
				padding = new RectOffset(12, 12, 12, 12),
				margin = new RectOffset(10, 10, 10, 10)
			});

			EditorGUILayout.TextArea(
				"This realm doesn't have any content yet. " +
				"\n\n" +
				"Would you like to import Beamable's default content? This creates the gems and coins " +
				"currencies and copies their icon sprites into Assets/Beamable/DefaultAssets, " +
				"registering them as Addressables. \"Import & Publish\" also publishes the content to " +
				"this realm.",
				new GUIStyle(EditorStyles.label) { wordWrap = true });

			EditorGUILayout.BeginHorizontal(new GUIStyle
			{
				margin = new RectOffset(0, 0, 12, 12)
			});

			EditorGUILayout.Space(5, true);
			EditorGUILayout.Space(5, true);

			var clickedNotNow = BeamGUI.CancelButton("Not now");
			var clickedImportAndPublish = BeamGUI.CancelButton("Import & Publish");
			var clickedImport = BeamGUI.PrimaryButton(new GUIContent("Import"));

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			if (clickedNotNow)
			{
				DefaultContentImporter.SetDismissed(_cli?.CurrentRealm?.Cid, _cli?.CurrentRealm?.Pid);
				Repaint();
			}
			else if (clickedImport)
			{
				StartDefaultContentImport(false);
			}
			else if (clickedImportAndPublish)
			{
				StartDefaultContentImport(true);
			}
		}

		private async void StartDefaultContentImport(bool publishAfter)
		{
			if (_importingDefaultContent)
				return;

			_importingDefaultContent = true;
			Repaint();
			try
			{
				// Awaited continuations resume on Unity's main-thread synchronization context.
				await DefaultContentImporter.ImportDefaultContent(publishAfter);
			}
			finally
			{
				_importingDefaultContent = false;
				Repaint();
			}
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

		public void DrawNestedContent(Action drawContent)
		{
			EditorGUILayout.BeginVertical(new GUIStyle()
			{
				padding = new RectOffset(NESTED_CONTENT_PADDING,NESTED_CONTENT_PADDING,NESTED_CONTENT_PADDING,NESTED_CONTENT_PADDING)
			});
			drawContent();
			EditorGUILayout.EndVertical();
		}
		public const int NESTED_CONTENT_PADDING = 24;
	}

	public enum ContentWindowStatus
	{
		Normal,
		Publish,
		Building,
		Revert,
		Validate,
		SnapshotManager,
	}
	
}
