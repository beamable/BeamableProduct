using Beamable.Common;
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
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussThemeManager : EditorWindow
	{
		private BussStyleListVisualElement _stylesGroup;
		private BussElementHierarchyVisualElement _navigationWindow;
		private LabeledCheckboxVisualElement _filterToggle;

		private bool _inStyleSheetChangedLoop;
		private readonly VariableDatabase _variableDatabase = new VariableDatabase();

		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();

		private AddStyleButton _addStyleButton;
		private bool _filterMode;
		private BeamablePopupWindow _confirmationPopup;
		private readonly List<BussStyleSheet> _activeStyleSheets = new List<BussStyleSheet>();

		public List<BussStyleSheet> ActiveStyleSheets => _activeStyleSheets;

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
				$"{BUSS_THEME_MANAGER_PATH}/BussThemeManager.uss");

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

			_stylesGroup = new BussStyleListVisualElement();
			_stylesGroup.name = "stylesGroup";
			_stylesGroup.Filter = CardFilter;
			scrollView.Add(_stylesGroup);

			root.Add(mainVisualElement);

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.HierarchyChanged += RefreshStyleSheets;

			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange += RefreshStyleSheets;

			RefreshStyleSheets();
			AddSelectorButton(scrollView);
		}

		private void OnFilterToggleClicked(bool value)
		{
			_filterMode = value;
			_stylesGroup.FilterCards();
		}

		private bool CardFilter(BussStyleSheet styleSheet, BussStyleRule styleRule)
		{
			var selected = Selection.activeGameObject;
			BussElement selectedElement = null;
			if (selected != null)
			{
				selectedElement = selected.GetComponent<BussElement>();
			}
			
			if (selectedElement == null || !_filterMode) return true;

			return (styleRule.Selector?.CheckMatch(_navigationWindow.SelectedComponent) ?? false);
		}

		private void RefreshStyleSheets()
		{
			_stylesGroup.StyleSheets = _navigationWindow.StyleSheets;
		}

		private void AddSelectorButton(VisualElement parent)
		{
			_addStyleButton = new AddStyleButton();
			_addStyleButton.Setup(this, _navigationWindow, _ => RefreshStyleSheets());
			_addStyleButton.CheckEnableState();
			parent.Insert(2, _addStyleButton);
		}

		private void OnFocus()
		{
			_navigationWindow?.ForceRebuild();
			_addStyleButton.CheckEnableState();
		}

		private void OnDestroy()
		{
			_filterToggle.OnValueChanged -= OnFilterToggleClicked;

			_navigationWindow.HierarchyChanged -= RefreshStyleSheets;
			_navigationWindow.BussStyleSheetChange -= RefreshStyleSheets;

			_navigationWindow.Destroy();
			UndoSystem<BussStyleRule>.DeleteAllRecords();
		}
		
		private void AddSelectorButton(VisualElement parent)
		{
			_addStyleButton = new AddStyleButton();
			_addStyleButton.Setup(this, _navigationWindow, _ => RefreshStyleSheets());
			_addStyleButton.CheckEnableState();
			parent.Insert(2, _addStyleButton);
		}

		private void OnFocus()
		{
			_navigationWindow?.ForceRebuild();
			_addStyleButton.CheckEnableState();
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
			styleCard.Setup(styleSheet, styleRule, _variableDatabase, callback);
			_styleCardsVisualElements.Add(styleCard);
			_stylesGroup.Add(styleCard);

			void OpenAddSelectorWindow()
			styleCard.EnterEditMode += () =>
			{
				if (_addStyleButton.ClassListContains(UIElementExtensions.PROPERTY_INACTIVE))
				foreach (BussStyleCardVisualElement other in _styleCardsVisualElements)
				{
					return;
					if (other != styleCard && other.EditMode)
					{
						other.SetEditMode(false);
					}
				}

				AddStyleWindow window = AddStyleWindow.ShowWindow();
				window?.Init(_ => RefreshStyleSheets(), _activeStyleSheets.ToList());
			}
				FilterCards();
			};
		}

		private void OnFocus()
		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_navigationWindow?.ForceRebuild();
			CheckEnableState();
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}

		private void CheckEnableState()
		private void OnStyleSheetExternallyChanged()
		{
			if (_addStyleButton == null) return;
			Profiler.BeginSample("BUSS - callback on style sheet change");

			_addStyleButton.tooltip = string.Empty;
			_activeStyleSheets.Clear();
			if (_inStyleSheetChangedLoop) return;

#if BEAMABLE_DEVELOPER
			_activeStyleSheets.AddRange(_navigationWindow.StyleSheets);
#else
			_activeStyleSheets.AddRange(_navigationWindow.StyleSheets.Where(bussStyleSheet => !bussStyleSheet.IsReadOnly));
#endif
			_inStyleSheetChangedLoop = true;

			if (_activeStyleSheets.Count == 0)
			try
			{
				_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
				_addStyleButton.SetInactive(true);
				_variableDatabase.ReconsiderAllStyleSheets();

				if (_variableDatabase.ForceRefreshAll || // if we did complex change and we need to refresh all styles
					_variableDatabase.DirtyProperties.Count == 0) // or if we did no changes (the source of change is unknown)
				{
					RefreshStyleCards();
				}
				else
				{
					var cardsToReloads = new List<BussStyleCardVisualElement>();
					foreach (VariableDatabase.PropertyReference reference in _variableDatabase.DirtyProperties)
					{
						var card = _styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == reference.styleRule);
						if (card != null)
						{
							if (card.CheckPropertyIsInStyle(reference))
								card.RefreshPropertyByReference(reference);
							else if (!cardsToReloads.Contains(card))
								cardsToReloads.Add(card);
						}
					}
					
					foreach (var card in cardsToReloads)
						card.Refresh();

					cardsToReloads.Clear();
				}
				_variableDatabase.FlushDirtyMarkers();
			}
			else
			catch (Exception e)
			{
				_addStyleButton.tooltip = String.Empty;
				_addStyleButton.SetInactive(false);
				Debug.LogException(e);
			}

			_inStyleSheetChangedLoop = false;

			Profiler.EndSample();
		}
	}
}
