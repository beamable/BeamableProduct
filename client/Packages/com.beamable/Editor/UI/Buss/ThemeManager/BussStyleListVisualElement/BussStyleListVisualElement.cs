using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleListVisualElement : BeamableBasicVisualElement
	{
		private readonly BussCardFilter _filter;
		private readonly ThemeManagerModel _model;

		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();

		private string _currentFilter;
		// private BussElement _currentSelected;

		private bool _inStyleSheetChangedLoop;

		private IEnumerable<BussStyleSheet> _styleSheets;

		public VariableDatabase VariableDatabase { get; } = new VariableDatabase();
		public PropertySourceDatabase PropertyDatabase { get; } = new PropertySourceDatabase();

		// public IEnumerable<BussStyleSheet> StyleSheets
		// {
		// 	get => _styleSheets;
		// 	set
		// 	{
		// 		ClearStyleSheets();
		// 		_styleSheets = value;
		// 		RefreshStyleSheets();
		// 	}
		// }

		public IEnumerable<BussStyleSheet> WritableStyleSheets
		{
			get
			{
#if BEAMABLE_DEVELOPER
				return _styleSheets ?? Enumerable.Empty<BussStyleSheet>();
#else
				return _styleSheets?.Where(s => !s.IsReadOnly) ?? Enumerable.Empty<BussStyleSheet>();
#endif
			}
		}

		public BussStyleListVisualElement(ThemeManagerModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleListVisualElement)}/{nameof(BussStyleListVisualElement)}.uss",
			false)
		{
			_model = model;

			_filter = new BussCardFilter();

			// Selection.selectionChanged += OnSelectionChange;

			_model.Change += Refresh;
		}

		public override void Refresh()
		{
			RefreshStyleCards();
		}

		public float GetSelectedElementPosInScroll()
		{
			if (_model.SelectedElement == null)
				return 0;

			int selectedIndex = -1;
			float selectedHeight = 0;

			for (int i = 0; i < _styleCardsVisualElements.Count; i++)
			{
				bool isMatch = _styleCardsVisualElements[i].StyleRule.Selector?.CheckMatch(_model.SelectedElement) ?? false;

				if (selectedIndex != -1)
					continue;

				if (isMatch)
					selectedIndex = i;
				else
					selectedHeight += _styleCardsVisualElements[i].contentRect.height;
			}

			return selectedHeight;
		}

		public void RefreshStyleCards()
		{
			UndoSystem<BussStyleRule>.Update();

			BussStyleRule[] rulesToDraw = _model.StyleSheets.SelectMany(ss => ss.Styles).ToArray();

			BussStyleCardVisualElement[] cardsToRemove = _styleCardsVisualElements
			                                             .Where(card => !rulesToDraw.Contains(card.StyleRule))
			                                             .ToArray();

			foreach (BussStyleCardVisualElement card in cardsToRemove)
			{
				RemoveStyleCard(card);
			}

			foreach (BussStyleSheet styleSheet in _model.StyleSheets)
			{
				foreach (BussStyleRule rule in styleSheet.Styles)
				{
					BussStyleCardVisualElement spawned =
						_styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == rule);
					if (spawned != null)
					{
						spawned.RefreshProperties();
						spawned.RefreshButtons();
						spawned.RefreshWritableStyleSheets(WritableStyleSheets);
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

			FilterCards();
		}

		public void SetFilter(string value)
		{
			_filter.CurrentFilter = value;
			FilterCards();
		}

		protected override void OnDestroy()
		{
			_model.Change -= Refresh;
			// Selection.selectionChanged -= OnSelectionChange;
			PropertyDatabase.Discard();
		}

		private void AddStyleCard(BussStyleSheet styleSheet, BussStyleRule styleRule, Action callback)
		{
			BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
			styleCard.Setup(styleSheet, styleRule, VariableDatabase, PropertyDatabase, callback, WritableStyleSheets);
			_styleCardsVisualElements.Add(styleCard);
			Root.Add(styleCard);
		}

		private void ClearStyleSheets()
		{
			if (_model.StyleSheets != null)
			{
				foreach (BussStyleSheet styleSheet in _model.StyleSheets)
				{
					styleSheet.Change -= OnStyleSheetChanged;
				}

				_styleSheets = null;
			}
		}

		private void FilterCards()
		{
			foreach (BussStyleCardVisualElement styleCardVisualElement in _styleCardsVisualElements)
			{
				bool isVisible = _filter.CardFilter(styleCardVisualElement.StyleRule, _model.SelectedElement);
				styleCardVisualElement.SetHidden(!isVisible);
			}
		}

		// private void OnSelectionChange()
		// {
		// 	_currentSelected = null;
		// 	var gameObject = Selection.activeGameObject;
		// 	if (gameObject != null)
		// 	{
		// 		_currentSelected = gameObject.GetComponent<BussElement>();
		// 	}
		//
		// 	foreach (var styleCard in _styleCardsVisualElements)
		// 	{
		// 		styleCard.OnBussElementSelected(_currentSelected);
		// 	}
		//
		// 	FilterCards();
		// }

		private void OnStyleSheetChanged()
		{
			if (_inStyleSheetChangedLoop) return;

			_inStyleSheetChangedLoop = true;

			try
			{
				VariableDatabase.ReconsiderAllStyleSheets();

				if (VariableDatabase.ForceRefreshAll || // if we did complex change and we need to refresh all styles
				    VariableDatabase.DirtyProperties.Count ==
				    0) // or if we did no changes (the source of change is unknown)
				{
					RefreshStyleCards();
				}
				else
				{
					foreach (VariableDatabase.PropertyReference reference in VariableDatabase.DirtyProperties)
					{
						var card = _styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == reference.styleRule);
						if (card != null)
						{
							card.RefreshPropertyByReference(reference);
						}
					}
				}

				VariableDatabase.FlushDirtyMarkers();
			}
			catch (Exception e)
			{
				BeamableLogger.LogException(e);
			}

			_inStyleSheetChangedLoop = false;
		}

		private void RefreshStyleSheets()
		{
			VariableDatabase.RemoveAllStyleSheets();

			foreach (BussStyleSheet styleSheet in _model.StyleSheets)
			{
				VariableDatabase.AddStyleSheet(styleSheet);
				styleSheet.Change += OnStyleSheetChanged;
			}

			RefreshStyleCards();
		}

		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}
	}
}
