using Beamable.UI.Buss;
using System;
using System.Linq;

namespace Beamable.Editor.UI.Buss
{
	public class BussCardFilter
	{
		public string CurrentFilter { get; set; } = String.Empty;
		
		public bool CardFilter(BussStyleRule styleRule, BussElement selectedElement)
		{
			bool contains = styleRule.Properties.Any(property => property.Key.ToLower().Contains(CurrentFilter));

			return selectedElement == null
				? CurrentFilter.Length <= 0 || contains
				: styleRule.Selector != null && styleRule.Selector.CheckMatch(selectedElement) && contains;
		}
	}
}
