using Beamable.Common.Constants;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.BeamableConstants;
using static Beamable.Common.Constants.BeamableConstants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
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
		private List<BussStyleSheet> _activeStyleSheets = new List<BussStyleSheet>();

		[MenuItem(
			MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Commons.OPEN + " " +
			MenuItems.Windows.Names.THEME_MANAGER,
			priority = MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2 + 5)]
		public static void Init()
		{
			Type inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			BussThemeManager themeManagerWindow = GetWindow<BussThemeManager>(MenuItems.Windows.Names.THEME_MANAGER, true, inspector);
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
			minSize = THEME_MANAGER_WINDOW_SIZE;
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

			mainVisualElement.AddStyleSheet(
				$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");

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
			_filterToggle.SetWithoutNotify(_filterMode);
			scrollView.Add(_filterToggle);

			_stylesGroup = new VisualElement();
			_stylesGroup.name = "stylesGroup";
			scrollView.Add(_stylesGroup);

			root.Add(mainVisualElement);

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.HierarchyChanged += RefreshStyleSheets;

			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange += RefreshStyleSheets;

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
			Profiler.BeginSample("BUSS - filtering style cards");
			if (_navigationWindow.SelectedComponent == null)
			{
				foreach (BussStyleCardVisualElement styleCardVisualElement in _styleCardsVisualElements)
				{
					styleCardVisualElement.SetHidden(false);
				}

				return;
			}

			foreach (BussStyleCardVisualElement styleCardVisualElement in _styleCardsVisualElements)
			{
				bool isMatch =
					styleCardVisualElement.StyleRule.Selector?.CheckMatch(_navigationWindow.SelectedComponent) ?? false;
				styleCardVisualElement.SetHidden(_filterMode && !isMatch && !styleCardVisualElement.EditMode);
			}
			Profiler.EndSample();
		}

		private void RefreshStyleSheets()
		{
			Profiler.BeginSample("BUSS - refreshing style sheets");

			ClearCurrentStyleSheet();

			_variableDatabase.RemoveAllStyleSheets();

			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				_variableDatabase.AddStyleSheet(styleSheet);
				styleSheet.Change += OnStyleSheetExternallyChanged;
			}

			Profiler.EndSample();

			RefreshStyleCards();
		}

		private void RefreshStyleCards()
		{
			Profiler.BeginSample("BUSS - refreshing style cards");

			UndoSystem<BussStyleRule>.Update();

			BussStyleRule[] rulesToDraw = _navigationWindow.StyleSheets.SelectMany(ss => ss.Styles).ToArray();

			BussStyleCardVisualElement[] cardsToRemove = _styleCardsVisualElements.Where(card => !rulesToDraw.Contains(card.StyleRule))
														 .ToArray();

			foreach (BussStyleCardVisualElement card in cardsToRemove)
			{
				RemoveStyleCard(card);
			}

			foreach (BussStyleSheet styleSheet in _navigationWindow.StyleSheets)
			{
				foreach (BussStyleRule rule in styleSheet.Styles)
				{
					BussStyleCardVisualElement spawned = _styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == rule);
					if (spawned != null)
					{
						spawned.RefreshProperties();
						spawned.RefreshButtons();
					}
					else
					{
						string undoKey = $"{styleSheet.name}-{rule.SelectorString}";
						UndoSystem<BussStyleRule>.AddRecord(rule, undoKey);
						AddStyleCard(styleSheet, rule, () =>
						{
							UndoSystem<BussStyleRule>.Undo(undoKey);
							RefreshStyleCards();
						});
					}
				}
			}

			Profiler.EndSample();

			FilterCards();
		}

		private void AddSelectorButton()
		{
			_addStyleButton = new VisualElement { name = "addStyleButton" };
			_addStyleButton.AddToClassList("button");
			_addStyleButton.Add(new Label("Add Style"));
			_addStyleButton.UnregisterCallback<MouseDownEvent>(_ => OpenAddSelectorWindow());
			_addStyleButton.RegisterCallback<MouseDownEvent>(_ => OpenAddSelectorWindow());

			_addStyleButton.UnregisterCallback<MouseEnterEvent>(_ => CheckEnableState());
			_addStyleButton.RegisterCallback<MouseEnterEvent>(_ => CheckEnableState());
			CheckEnableState();

			_stylesGroup.Add(_addStyleButton);

			void OpenAddSelectorWindow()
			{
				if (_addStyleButton.ClassListContains(UIElementExtensions.PROPERTY_INACTIVE))
				{
					return;
				}

				AddStyleWindow window = AddStyleWindow.ShowWindow();
				window?.Init(_ => RefreshStyleSheets(), _activeStyleSheets.ToList());
			}
		}

		private void OnFocus()
		{
			_navigationWindow?.ForceRebuild();
			CheckEnableState();
		}

		private void CheckEnableState()
		{
			if (_addStyleButton == null) return;

			_addStyleButton.tooltip = string.Empty;
			_activeStyleSheets.Clear();

#if BEAMABLE_DEVELOPER
			_activeStyleSheets.AddRange(_navigationWindow.StyleSheets);
#else
			_activeStyleSheets.AddRange(_navigationWindow.StyleSheets.Where(bussStyleSheet => !bussStyleSheet.IsReadOnly));
#endif

			if (_activeStyleSheets.Count == 0)
			{
				_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
				_addStyleButton.SetInactive(true);
			}
			else
			{
				_addStyleButton.tooltip = String.Empty;
				_addStyleButton.SetInactive(false);
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

			styleCard.EnterEditMode += () =>
			{
				foreach (BussStyleCardVisualElement other in _styleCardsVisualElements)
				{
					if (other != styleCard && other.EditMode)
					{
						other.SetEditMode(false);
					}
				}
				FilterCards();
			};
		}

		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}

		private void OnStyleSheetExternallyChanged()
		{
			Profiler.BeginSample("BUSS - callback on style sheet change");

			if (_inStyleSheetChangedLoop) return;

			_inStyleSheetChangedLoop = true;

			try
			{
				_variableDatabase.ReconsiderAllStyleSheets();

				if (_variableDatabase.ForceRefreshAll || // if we did complex change and we need to refresh all styles
					_variableDatabase.DirtyProperties.Count == 0) // or if we did no changes (the source of change is unknown)
				{
					RefreshStyleCards();
				}
				else
				{
					foreach (VariableDatabase.PropertyReference reference in _variableDatabase.DirtyProperties)
					{
						var card = _styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == reference.styleRule);
						if (card != null)
						{
							card.RefreshPropertyByReference(reference);
						}
					}
				}
				_variableDatabase.FlushDirtyMarkers();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			_inStyleSheetChangedLoop = false;

			Profiler.EndSample();
		}

		private void OnDestroy()
		{
			ClearCurrentStyleSheet();

			_filterToggle.OnValueChanged -= OnFilterToggleClicked;

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			_navigationWindow.SelectionChanged -= FilterCards;

			_navigationWindow.Destroy();
			UndoSystem<BussStyleRule>.DeleteAllRecords();
		}
	}
}
