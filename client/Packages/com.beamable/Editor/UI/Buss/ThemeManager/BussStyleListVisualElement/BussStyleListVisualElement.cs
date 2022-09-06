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
		private bool _inStyleSheetChangedLoop;

		public VariableDatabase VariableDatabase { get; } = new VariableDatabase();
		public PropertySourceDatabase PropertyDatabase { get; } = new PropertySourceDatabase();

		public BussStyleListVisualElement(ThemeManagerModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleListVisualElement)}/{nameof(BussStyleListVisualElement)}.uss",
			false)
		{
			_model = model;

			_filter = new BussCardFilter();
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
				bool isMatch = _styleCardsVisualElements[i].StyleRule.Selector?.CheckMatch(_model.SelectedElement) ??
				               false;

				if (selectedIndex != -1)
					continue;

				if (isMatch)
					selectedIndex = i;
				else
					selectedHeight += _styleCardsVisualElements[i].contentRect.height;
			}

			return selectedHeight;
		}

		// TODO: change way how this works, we should render cards that are filtered inside model
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
						spawned.RefreshWritableStyleSheets(_model.WritableStyleSheets);
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
			PropertyDatabase.Discard();
		}

		private void AddStyleCard(BussStyleSheet styleSheet, BussStyleRule styleRule, Action callback)
		{
			BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
			styleCard.Setup(styleSheet, styleRule, VariableDatabase, PropertyDatabase, callback,
			                _model.WritableStyleSheets);
			_styleCardsVisualElements.Add(styleCard);
			Root.Add(styleCard);
		}

		private void FilterCards()
		{
			foreach (BussStyleCardVisualElement styleCardVisualElement in _styleCardsVisualElements)
			{
				bool isVisible = _filter.CardFilter(styleCardVisualElement.StyleRule, _model.SelectedElement);
				styleCardVisualElement.SetHidden(!isVisible);
			}
		}

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

		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}
	}
}
