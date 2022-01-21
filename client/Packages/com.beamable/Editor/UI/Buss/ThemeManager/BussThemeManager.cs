using Beamable.Editor;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.BUSS.ThemeManager;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.UI.BUSS
{
	public class BussThemeManager : EditorWindow
	{
		private VisualElement _stylesGroup;
		private BussElementHierarchyVisualElement _navigationWindow;
		private LabeledCheckboxVisualElement _filterToggle;

		private bool _inStyleSheetChangedLoop;
		private readonly VariableDatabase _variableDatabase = new VariableDatabase();

		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();

		private VisualElement _addStyleButton;
		private bool _filterMode;
		private BeamablePopupWindow _confirmationPopup;

#if BEAMABLE_DEVELOPER
		// [MenuItem(
		// 	BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER + "/" +
		// 	BeamableConstants.OPEN + " " +
		// 	BeamableConstants.THEME_MANAGER,
		// 	priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
		[MenuItem("Private/Theme Manager")]
#endif
		public static void Init()
		{
			BussThemeManager themeManagerWindow = GetWindow<BussThemeManager>(BeamableConstants.THEME_MANAGER, true);
			themeManagerWindow.Show(true);
		}

		public void CloseConfirmationPopup()
		{
			if (_confirmationPopup != null)
			{
				_confirmationPopup.Close();
			}

			_confirmationPopup = null;
		}

		public void SetConfirmationPopup(BeamablePopupWindow popupWindow)
		{
			_confirmationPopup = popupWindow;
		}

		private void OnEnable()
		{
			minSize = BussConstants.ThemeManagerWindowSize;
			Refresh();
		}

		private void Refresh()
		{
			VisualElement root = this.GetRootVisualContainer();
			root.Clear();
			_styleCardsVisualElements.Clear();
			_addStyleButton = null;

			VisualElement mainVisualElement = new VisualElement();
			mainVisualElement.name = "themeManagerContainer";

#if UNITY_2018
			mainVisualElement.AddStyleSheet(
				$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussThemeManager.2018.uss");
#elif UNITY_2019_1_OR_NEWER
			mainVisualElement.AddStyleSheet(
				$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");
#endif

			ScrollView scrollView = new ScrollView();
			scrollView.name = "themeManagerContainerScrollView";
			mainVisualElement.Add(scrollView);

			VisualElement navigationGroup = new VisualElement();
			navigationGroup.name = "navigationGroup";
			scrollView.Add(navigationGroup);

			_navigationWindow = new BussElementHierarchyVisualElement();
			_navigationWindow.Init();
			navigationGroup.Add(_navigationWindow);

			_filterToggle = new LabeledCheckboxVisualElement("Filter by selected element");
			_filterToggle.name = "filterToggle";
			_filterToggle.OnValueChanged -= OnFilterToggleClicked;
			_filterToggle.OnValueChanged += OnFilterToggleClicked;
			_filterToggle.Refresh();
			scrollView.Add(_filterToggle);

			_stylesGroup = new VisualElement();
			_stylesGroup.name = "stylesGroup";
			scrollView.Add(_stylesGroup);

			root.Add(mainVisualElement);

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.HierarchyChanged += RefreshStyleSheets;

			_navigationWindow.SelectionChanged -= FilterCards;
			_navigationWindow.SelectionChanged += FilterCards;

			RefreshStyleSheets();
			AddSelectorButton();
		}

		private void OnFilterToggleClicked(bool value)
		{
			_filterMode = value;
			FilterCards();
		}

		private void FilterCards(GameObject _ = null)
		{
			foreach (BussStyleCardVisualElement styleCardVisualElement in _styleCardsVisualElements)
			{
				bool isMatch =
					styleCardVisualElement.StyleRule.Selector.CheckMatch(_navigationWindow.SelectedComponent);
				styleCardVisualElement.SetHidden(_filterMode && !isMatch);
			}
		}

		private void RefreshStyleSheets()
		{
			ClearCurrentStyleSheet();

			_variableDatabase.RemoveAllStyleSheets();

			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				_variableDatabase.AddStyleSheet(styleSheet);
				styleSheet.Change += OnStyleSheetExternallyChanged;
			}

			RefreshStyleCards();
		}

		private void RefreshStyleCards()
		{
			UndoSystem<BussStyleRule>.Update();

			var rulesToDraw = _navigationWindow.StyleSheets.SelectMany(ss => ss.Styles).ToArray();

			var cardsToRemove = _styleCardsVisualElements.Where(card => !rulesToDraw.Contains(card.StyleRule))
														 .ToArray();

			foreach (BussStyleCardVisualElement card in cardsToRemove)
			{
				RemoveStyleCard(card);
			}

			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				foreach (BussStyleRule rule in styleSheet.Styles)
				{
					var spawned = _styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == rule);
					if (spawned != null)
					{
						spawned.RefreshProperties();
					}
					else
					{
						var undoKey = $"{styleSheet.name}-{rule.SelectorString}";
						UndoSystem<BussStyleRule>.AddRecord(rule, undoKey);
						AddStyleCard(styleSheet, rule, () =>
						{
							UndoSystem<BussStyleRule>.Undo(undoKey);
							RefreshStyleCards();
						});
					}
				}
			}
		}

		private void AddSelectorButton()
		{
			_addStyleButton = new VisualElement { name = "addStyleButton" };
			_addStyleButton.AddToClassList("button");
			_addStyleButton.Add(new Label("Add Style"));
			_addStyleButton.UnregisterCallback<MouseDownEvent>(_ => OpenAddSelectorWindow());
			_addStyleButton.RegisterCallback<MouseDownEvent>(_ => OpenAddSelectorWindow());

			EditorApplication.update -= CheckEnableState;
			EditorApplication.update += CheckEnableState;

			_stylesGroup.Add(_addStyleButton);

			void OpenAddSelectorWindow()
			{
				AddStyleWindow window = AddStyleWindow.ShowWindow();
				window?.Init(_ => RefreshStyleSheets());
			}

			void CheckEnableState()
			{
				_addStyleButton.tooltip = string.Empty;
				List<BussStyleSheet> styleSheets =
					Helper.FindAssets<BussStyleSheet>("t:BussStyleSheet", new[] { "Assets" });
				if (styleSheets.Count == 0)
				{
					_addStyleButton.tooltip = "There should be created at least one BUSS Style Config!";
					_addStyleButton.SetEnabled(false);
				}
				else
				{
					_addStyleButton.SetEnabled(true);
				}
			}
		}

		private void ClearCurrentStyleSheet()
		{
			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				styleSheet.Change -= OnStyleSheetExternallyChanged;
			}

			_variableDatabase.RemoveAllStyleSheets();
		}

		private void AddStyleCard(BussStyleSheet styleSheet, BussStyleRule styleRule, Action callback)
		{
			BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
			styleCard.Setup(this, styleSheet, styleRule, _variableDatabase, _navigationWindow, callback);
			_styleCardsVisualElements.Add(styleCard);
			_stylesGroup.Add(styleCard);

			if (_addStyleButton != null)
			{
				_addStyleButton.PlaceInFront(styleCard);
			}
		}

		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}

		private void OnStyleSheetExternallyChanged()
		{
			if (_inStyleSheetChangedLoop) return;

			_inStyleSheetChangedLoop = true;

			try
			{
				_variableDatabase.ReconsiderAllStyleSheets();

				RefreshStyleCards();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			_inStyleSheetChangedLoop = false;
		}

		private void OnDestroy()
		{
			ClearCurrentStyleSheet();

			_filterToggle.OnValueChanged -= OnFilterToggleClicked;

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.SelectionChanged -= FilterCards;

			_navigationWindow.Destroy();
			UndoSystem<BussStyleRule>.DeleteAllRecords();
		}
	}
}
