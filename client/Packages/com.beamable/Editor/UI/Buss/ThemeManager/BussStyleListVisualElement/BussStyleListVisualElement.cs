using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using UnityEditor;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleListVisualElement : BeamableBasicVisualElement
	{
		public Func<BussStyleSheet, BussStyleRule, bool> Filter;
		
		private readonly List<BussStyleCardVisualElement> _styleCardsVisualElements =
			new List<BussStyleCardVisualElement>();
		private readonly VariableDatabase _variableDatabase = new VariableDatabase();
		private bool _inStyleSheetChangedLoop;
		
		private IEnumerable<BussStyleSheet> _styleSheets;

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
				return _styleSheets;
#else
				return _styleSheets.Where(s => !s.IsReadOnly);
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
			_variableDatabase.RemoveAllStyleSheets();

			foreach (BussStyleSheet styleSheet in StyleSheets)
			{
				_variableDatabase.AddStyleSheet(styleSheet);
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
				bool visible =
					Filter?.Invoke(styleCardVisualElement.StyleSheet, styleCardVisualElement.StyleRule) ?? true;
				styleCardVisualElement.SetHidden(!visible);
			}
		}

		private void AddStyleCard(BussStyleSheet styleSheet, BussStyleRule styleRule, Action callback)
		{
			BussStyleCardVisualElement styleCard = new BussStyleCardVisualElement();
			styleCard.Setup(styleSheet, styleRule, _variableDatabase, callback);
			_styleCardsVisualElements.Add(styleCard);
			Root.Add(styleCard);

			styleCard.EnterEditMode += () =>
			{
				foreach (BussStyleCardVisualElement other in _styleCardsVisualElements)
				{
					if (other != styleCard && other.EditMode)
					{
						other.SetEditMode(false);
					}
				}
			};
		}

		private void RemoveStyleCard(BussStyleCardVisualElement card)
		{
			_styleCardsVisualElements.Remove(card);
			card.RemoveFromHierarchy();
			card.Destroy();
		}

		private void OnStyleSheetChanged()
		{if (_inStyleSheetChangedLoop) return;

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
				BeamableLogger.LogException(e);
			}

			_inStyleSheetChangedLoop = false;
		}

		private void OnSelectionChange()
		{
			BussElement element = null;
			var gameObject = Selection.activeGameObject;
			if (gameObject != null)
			{
				var el = gameObject.GetComponent<BussElement>();
				if (el != null)
				{
					element = el;
				}
			}
			
			foreach (var styleCard in _styleCardsVisualElements)
			{
				styleCard.OnBussElementSelected(element);
			}
			FilterCards();
		}

		protected override void OnDestroy()
		{
			Selection.selectionChanged -= OnSelectionChange;
		}
	}
}
