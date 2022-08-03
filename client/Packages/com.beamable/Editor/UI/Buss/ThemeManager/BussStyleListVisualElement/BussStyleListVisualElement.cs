using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleListVisualElement : BeamableBasicVisualElement
	{
		public Func<BussStyleSheet, BussStyleRule, bool> Filter;

		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();

		public VariableDatabase VariableDatabase { get; } = new VariableDatabase();
		public PropertySourceDatabase PropertyDatabase { get; } = new PropertySourceDatabase();

		private bool _inStyleSheetChangedLoop;

		private IEnumerable<BussStyleSheet> _styleSheets;
		private BussElement _currentSelected;

		public IEnumerable<BussStyleSheet> StyleSheets
		{
			get => _styleSheets;
			set
			{
				ClearStyleSheets();
				_styleSheets = value;
				RefreshStyleSheets();
			}
		}

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

		public BussStyleListVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleListVisualElement)}/{nameof(BussStyleListVisualElement)}.uss", false)
		{
			Init();
			Selection.selectionChanged += OnSelectionChange;
		}

		private void RefreshStyleSheets()
		{
			VariableDatabase.RemoveAllStyleSheets();

			foreach (BussStyleSheet styleSheet in StyleSheets)
			{
				VariableDatabase.AddStyleSheet(styleSheet);
				styleSheet.Change += OnStyleSheetChanged;
			}

			RefreshStyleCards();
		}

		private void ClearStyleSheets()
		{
			if (StyleSheets != null)
			{
				foreach (BussStyleSheet styleSheet in StyleSheets)
				{
					styleSheet.Change -= OnStyleSheetChanged;
				}

				_styleSheets = null;
			}
		}

		public void RefreshStyleCards()
		{
			UndoSystem<BussStyleRule>.Update();

			BussStyleRule[] rulesToDraw = StyleSheets.SelectMany(ss => ss.Styles).ToArray();

			BussStyleCardVisualElement[] cardsToRemove = _styleCardsVisualElements.Where(card => !rulesToDraw.Contains(card.StyleRule))
																				  .ToArray();

			foreach (BussStyleCardVisualElement card in cardsToRemove)
			{
				RemoveStyleCard(card);
			}

			foreach (BussStyleSheet styleSheet in StyleSheets)
			{
				foreach (BussStyleRule rule in styleSheet.Styles)
				{
					BussStyleCardVisualElement spawned = _styleCardsVisualElements.FirstOrDefault(c => c.StyleRule == rule);
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

		public void FilterCards()
		{
			foreach (BussStyleCardVisualElement styleCardVisualElement in _styleCardsVisualElements)
			{
				bool isVisible =
					Filter?.Invoke(styleCardVisualElement.StyleSheet, styleCardVisualElement.StyleRule) ?? true;
				styleCardVisualElement.SetHidden(!isVisible);
			}
		}

		public float GetSelectedElementPosInScroll()
		{
			if (_currentSelected == null)
				return 0;

			int selectedIndex = -1;
			float selectedHeight = 0;

			for (int i = 0; i < _styleCardsVisualElements.Count; i++)
			{
				bool isMatch = _styleCardsVisualElements[i].StyleRule.Selector?.CheckMatch(_currentSelected) ?? false;

				if (selectedIndex != -1)
					continue;

				if (isMatch)
					selectedIndex = i;
				else
					selectedHeight += _styleCardsVisualElements[i].contentRect.height;
			}

			return selectedHeight;
		}

		private void AddStyleCard(BussStyleSheet styleSheet, BussStyleRule styleRule, Action callback)
		{
			BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
			styleCard.Setup(styleSheet, styleRule, VariableDatabase, PropertyDatabase, callback, WritableStyleSheets);
			_styleCardsVisualElements.Add(styleCard);
			Root.Add(styleCard);
		}

		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}

		private void OnStyleSheetChanged()
		{
			if (_inStyleSheetChangedLoop) return;

			_inStyleSheetChangedLoop = true;

			try
			{
				VariableDatabase.ReconsiderAllStyleSheets();

				if (VariableDatabase.ForceRefreshAll || // if we did complex change and we need to refresh all styles
					VariableDatabase.DirtyProperties.Count == 0) // or if we did no changes (the source of change is unknown)
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

		private void OnSelectionChange()
		{
			_currentSelected = null;
			var gameObject = Selection.activeGameObject;
			if (gameObject != null)
			{
				_currentSelected = gameObject.GetComponent<BussElement>();
			}

			foreach (var styleCard in _styleCardsVisualElements)
			{
				styleCard.OnBussElementSelected(_currentSelected);
			}
			FilterCards();
		}

		protected override void OnDestroy()
		{
			Selection.selectionChanged -= OnSelectionChange;
			PropertyDatabase.Discard();
		}
	}
}
