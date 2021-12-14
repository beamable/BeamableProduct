using Beamable.Common;
using Beamable.CronExpression;
using Beamable.Editor.BeamableAssistant.Components;
using Beamable.Editor.BeamableAssistant.Models;
using Beamable.Editor.Content;
using Beamable.Editor.Content.Components;
using Beamable.Editor.UI.Components;
using Common.Runtime.BeamHints;
using Editor.BeamableAssistant.BeamHints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using ActionBarVisualElement = Beamable.Editor.BeamableAssistant.Components.ActionBarVisualElement;

namespace Beamable.Editor.BeamableAssistant
{
	public class BeamableAssistantWindow : EditorWindow, ISerializationCallbackReceiver
	{
		[MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
		          BeamableConstants.OPEN + " " +
		          BeamableConstants.BEAMABLE_ASSISTANT,
		          priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
		public static void ShowWindow()
		{
			var window = GetWindow<BeamableAssistantWindow>(BeamableConstants.BEAMABLE_ASSISTANT, true, typeof(SceneView));
			window.Show();
		}

		private readonly Vector2 MIN_SIZE = new Vector2(450, 200);

		private VisualElement _windowRoot;

		// Navigation
		private ActionBarVisualElement _actionBarVisualElement;

		// Assistant Work Area
		private VisualElement _assistantContainer;

		// Beam Hints Mode
		private VisualElement _domainTreeContainer;
		private VisualElement _hintsContainer;
		private IMGUIContainer _imguiContainer;
		private TreeViewIMGUI _treeViewIMGUI;
		[SerializeField] private TreeViewState _treeViewState;
		private SearchBarVisualElement _hintsSearchBar;
		
		
		
		// References to data 
		[SerializeField] private BeamHintsDataModel _beamHintsDataModel;
		private BeamHintDetailsReflectionCache.Registry _hintDetailsReflectionCache;

		private EditorAPI _editorAPI;

		private void OnEnable()
		{
			// TODO: Poll for changes in BeamHintGlobalStorage (add a per-domain dirty flag --- only care about the selected domains).
			EditorAPI.Instance.Then(Refresh);
		}

		private void OnFocus()
		{
			EditorAPI.Instance.Then(Refresh);
		}

		void Refresh(EditorAPI editorAPI)
		{
			minSize = MIN_SIZE;
			_editorAPI = editorAPI;
			_hintDetailsReflectionCache = editorAPI.EditorReflectionCache.GetFirstRegisteredUserSystemOfType<BeamHintDetailsReflectionCache.Registry>();

			var beamHintsDataModel = _beamHintsDataModel = _beamHintsDataModel ?? new BeamHintsDataModel();
			beamHintsDataModel.SetGlobalStorage(editorAPI.HintGlobalStorage);
			beamHintsDataModel.SetPreferencesManager(editorAPI.HintPreferencesManager);

			var root = this.GetRootVisualContainer();
			root.Clear();

			var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BeamableAssistantConstants.BASE_PATH}/BeamableAssistantWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{BeamableAssistantConstants.BASE_PATH}/BeamableAssistantWindow.uss");
			_windowRoot.name = nameof(_windowRoot);

			root.Add(_windowRoot);
			
			// Setup Assistant Visuals
			{
				_assistantContainer = root.Q<VisualElement>("assistant-container");
			}

			// Setup Beam Hints Mode Visuals
			{
				//Create IMGUI, The VisualElement Wrapper, and add to the parent
				_domainTreeContainer = root.Q<VisualElement>("domain-tree-container");
				_domainTreeContainer.RegisterCallback(new EventCallback<MouseUpEvent>(evt => {
					// Clears tree selection
					_treeViewState.selectedIDs.Clear();
					_treeViewIMGUI.Repaint();

					// Select all domains.
					_beamHintsDataModel.SelectDomains(new List<string>());
					FillDisplayingBeamHints(_hintsContainer, beamHintsDataModel.DisplayingHints);
				}));

				_treeViewState = _treeViewState ?? new TreeViewState();
				_treeViewIMGUI = new TreeViewIMGUI(_treeViewState) {SelectionType = SelectionType.Multiple, TreeViewItemRoot = new TreeViewItem {id = 0, depth = -1, displayName = "Root"}};
				_treeViewIMGUI.RowHeight = 30f;
				_imguiContainer = new IMGUIContainer(() => {
					// Tree view - Re-render every frame
					Rect rect = GUILayoutUtility.GetRect(200,
					                                     200,
					                                     _treeViewIMGUI.GetCalculatedHeight(),
					                                     _treeViewIMGUI.GetCalculatedHeight());

					_treeViewIMGUI.OnGUI(rect);
				}) {name = "domain-tree-imgui"};
				_domainTreeContainer.Add(_imguiContainer);

				// Get Hints View
				_hintsContainer = root.Q<VisualElement>("hints-container");

				// Setup Search Bar to filter Displaying Hints
				_hintsSearchBar = root.Q<SearchBarVisualElement>("hintsSearchBar");
				_hintsSearchBar.OnSearchChanged += delegate(string searchText) {
					_beamHintsDataModel.FilterDisplayedBy(searchText);
					FillDisplayingBeamHints(_hintsContainer, beamHintsDataModel.DisplayingHints);
				};
				
				SetupTreeViewCallbacks(
					_treeViewIMGUI,
					() => {
						BeamableLogger.Log("Context Clicked");
					},
					list => {
						BeamableLogger.Log($"Selection Change: {string.Join(", ", list.Select(a => a.ToString()))}");
						var allDomains = list
						                 .SelectMany(a => a.children != null ? a.children.Cast<BeamHintDomainTreeViewItem>() : new List<BeamHintDomainTreeViewItem>())
						                 .Concat(list.Cast<BeamHintDomainTreeViewItem>())
						                 .Select(item => item.FullDomain).ToList();

						beamHintsDataModel.SelectDomains(allDomains);
						FillDisplayingBeamHints(_hintsContainer, beamHintsDataModel.DisplayingHints);
					},
					list => {
						BeamableLogger.Log($"Selection Branch Change: {string.Join(", ", list.Select(a => a.ToString()))}");
					}
				);

				beamHintsDataModel.SelectDomains(beamHintsDataModel.SelectedDomains);
				FillTreeViewFromDomains(_treeViewIMGUI, beamHintsDataModel.SortedDomainsInStorage);
				FillDisplayingBeamHints(_hintsContainer, beamHintsDataModel.DisplayingHints);
				_imguiContainer?.MarkDirtyLayout();
				_imguiContainer?.MarkDirtyRepaint();
			}
			root.MarkDirtyRepaint();
		}

		public void FillDisplayingBeamHints(VisualElement container, List<BeamHintHeader> hintHeaders)
		{
			container.Clear();
			for (var headerIdx = 0; headerIdx < hintHeaders.Count; headerIdx++)
			{
				var beamHintHeader = hintHeaders[headerIdx];
				var hintVisualElement = new BeamHintHeaderVisualElement(_beamHintsDataModel, _hintDetailsReflectionCache, beamHintHeader, headerIdx);

				hintVisualElement.Refresh();
				hintVisualElement.UpdateFromBeamHintHeader(in beamHintHeader, headerIdx);

				container.Add(hintVisualElement);
			}
		}

		public void FillTreeViewFromDomains(TreeViewIMGUI imgui, List<string> sortedDomains)
		{
			var treeViewItems = new List<BeamHintDomainTreeViewItem>();
			var parentCache = new Dictionary<string, BeamHintDomainTreeViewItem>();

			var id = 1;
			foreach (string domain in sortedDomains)
			{
				var currDomainsDepth = BeamHintDomains.GetDomainDepth(domain);
				// Create parents for domain when necessary.
				for (int parentDepth = 0; parentDepth <= currDomainsDepth; parentDepth++)
				{
					if (!BeamHintDomains.TryGetDomainAtDepth(domain, parentDepth, out var parentDomain)) continue;

					var parentDomainStartIdx = domain.LastIndexOf(parentDomain, StringComparison.Ordinal);
					var domainSubstring = domain.Substring(0, parentDomainStartIdx + parentDomain.Length);

					// Guarantee uniqueness within first layer of domains
					if (!parentCache.TryGetValue(domainSubstring, out var item))
					{
						item = new BeamHintDomainTreeViewItem(id, parentDepth, domainSubstring, parentDomain);
						parentCache.Add(domainSubstring, item);
						treeViewItems.Add(item);
						id += 1;
					}
				}
			}

			imgui.TreeViewItems = treeViewItems.Cast<TreeViewItem>().ToList();
		}

		public void SetupTreeViewCallbacks(TreeViewIMGUI imgui,
		                                   Action onContextClicked,
		                                   Action<IList<TreeViewItem>> onSelectionChange,
		                                   Action<IList<TreeViewItem>> onSelectionBranchChange)
		{
			imgui.OnContextClicked += onContextClicked;
			imgui.OnSelectionChanged += onSelectionChange;
			imgui.OnSelectedBranchChanged += onSelectionBranchChange;
		}

		private void SetTreeViewItemsSafe(List<TreeViewItem> treeViewItem)
		{
			_treeViewIMGUI.TreeViewItems = treeViewItem;
			_imguiContainer?.MarkDirtyLayout();
			_imguiContainer?.MarkDirtyRepaint();
		}

		public void OnBeforeSerialize() { }

		public void OnAfterDeserialize() { }
	}
}
