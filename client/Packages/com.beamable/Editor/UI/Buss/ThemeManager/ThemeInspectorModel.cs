using Beamable.UI.Buss;
using System.Collections.Generic;

namespace Beamable.Editor.UI.Buss
{
	public class ThemeInspectorModel : ThemeModel
	{
		public override BussElement SelectedElement { get; set; }
		protected sealed override List<BussStyleSheet> StyleSheets { get; } = new List<BussStyleSheet>();
		public override Dictionary<BussStyleRule, BussStyleSheet> FilteredRules => Filter.GetFiltered(StyleSheets[0]);

		public ThemeInspectorModel(BussStyleSheet styleSheet)
		{
			Filter = new BussCardFilter();
			StyleSheets.Add(styleSheet);
		}
	}
}
