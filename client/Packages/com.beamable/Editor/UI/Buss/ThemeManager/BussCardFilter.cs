using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Editor.UI.Buss
{
	public class BussCardFilter
	{
		public string CurrentFilter { get; set; } = String.Empty;

		public Dictionary<BussStyleRule, BussStyleSheet> GetFiltered(BussStyleSheet styleSheet)
		{
			Dictionary<BussStyleRule, BussStyleSheet> rules = new Dictionary<BussStyleRule, BussStyleSheet>();

			foreach (var rule in styleSheet.Styles)
			{
				rules.Add(rule, styleSheet);
			}

			return rules;
		}

		public Dictionary<BussStyleRule, BussStyleSheet> GetFiltered(List<BussStyleSheet> styleSheets,
																	 BussElement selectedElement)
		{
			Dictionary<BussStyleRule, BussStyleSheet> rules = new Dictionary<BussStyleRule, BussStyleSheet>();

			foreach (var styleSheet in styleSheets)
			{
				foreach (var rule in styleSheet.Styles)
				{
					if (!CardFilter(rule, selectedElement))
					{
						continue;
					}

					if (!rules.ContainsKey(rule))
					{
						rules.Add(rule, styleSheet);
					}
				}
			}

			return rules;
		}

		private bool CardFilter(BussStyleRule styleRule, BussElement selectedElement)
		{
			bool contains = styleRule.Properties.Any(property => property.Key.ToLower().Contains(CurrentFilter)) ||
							styleRule.Properties.Count == 0;


			return selectedElement == null
				? CurrentFilter.Length <= 0 || contains
				: styleRule.Selector != null && styleRule.Selector.IsElementIncludedInSelector(selectedElement) && contains;
		}
	}
}
