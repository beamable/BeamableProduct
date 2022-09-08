using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleListVisualElement : BeamableBasicVisualElement
	{
		private readonly ThemeManagerModel _model;

		private readonly List<StyleCardVisualElement> _styleCardsVisualElements =
			new List<StyleCardVisualElement>();

		private bool _inStyleSheetChangedLoop;

		public BussStyleListVisualElement(ThemeManagerModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleListVisualElement)}/{nameof(BussStyleListVisualElement)}.uss",
			false)
		{
			_model = model;
			_model.Change += Refresh;
		}

		public override void Refresh()
		{
			RefreshCards();
		}

		protected override void OnDestroy()
		{
			_model.Change -= Refresh;
			
			ClearCards();
			
			_model.PropertyDatabase.Discard();
		}

		private void AddStyleCard(BussStyleSheet styleSheet, BussStyleRule styleRule, Action undoAction)
		{
			bool isSelected = _model.SelectedElement != null && styleRule.Selector.CheckMatch(_model.SelectedElement);
			StyleCardModel model =
				new StyleCardModel(styleSheet, styleRule, undoAction, _model.SelectedElement,  isSelected, _model.VariableDatabase, _model.PropertyDatabase, _model.WritableStyleSheets);
			StyleCardVisualElement styleCard = new StyleCardVisualElement(model);
			styleCard.Refresh();

			_styleCardsVisualElements.Add(styleCard);
			Root.Add(styleCard);
		}

		private void ClearCards()
		{
			foreach (var element in _styleCardsVisualElements)
			{
				RemoveStyleCard(element);
			}

			_styleCardsVisualElements.Clear();
		}

		private void RefreshCards()
		{
			UndoSystem<BussStyleRule>.Update();

			ClearCards();

			foreach (var pair in _model.FilteredRules)
			{
				var styleSheet = pair.Value;
				var styleRule = pair.Key;

				string undoKey = $"{styleSheet.name}-{styleRule.SelectorString}";
				UndoSystem<BussStyleRule>.AddRecord(styleRule, undoKey);
				AddStyleCard(styleSheet, styleRule, () =>
				{
					UndoSystem<BussStyleRule>.Undo(undoKey);
					RefreshCards(); // TODO: check if we need this refresh here
				});
			}
		}

		private void RemoveStyleCard(StyleCardVisualElement card)
		{
			card.RemoveFromHierarchy();
			card.Destroy();
		}
	}
}
