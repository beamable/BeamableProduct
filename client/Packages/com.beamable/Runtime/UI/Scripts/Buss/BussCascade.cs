using System.Collections.Generic;
using System.Linq;

namespace Beamable.UI.Buss
{
	public static class BussCascade
	{
		/// <summary>
		/// Assumes that start is the root of the world. No parent styles will be applied
		/// </summary>
		/// <param name="start"></param>
		public static void Cascade(StyleBehaviour start)
		{
			var config = BussConfiguration.Instance;

			var baseRules = new List<SelectorWithStyle>();
			foreach (var sheet in config.EnumerateSheets())
			{
				foreach (var rule in sheet.Rules)
				{
					baseRules.Add(rule);
				}
			}

			// the start element may not be the root, and so there may be inheritable styles from parent style sheets.
			var baseInheritRules = baseRules.ToList();
			foreach (var parent in start.Climb())
			{
				foreach (var sheet in parent.GetStyleSheets())
				{
					baseInheritRules.AddRange(sheet.Rules);
				}
			}

			// this is the recursive method that we can run for all children, starting with the given element.
			void Apply(StyleBehaviour element,
					   StyleObject inherit,
					   List<SelectorWithStyle> parentRules,
					   List<SelectorWithStyle> inheritableRules)
			{
				var selfRules = new List<SelectorWithStyle>();
				foreach (var sheet in element.StyleSheets)
				{
					foreach (var selfRule in sheet.Rules)
					{
						selfRules.Add(selfRule);
						// any rule that gets added can actually bring with it inherited rules that we wouldn't have already caught.
					}
				}

				// find and merge any styles from this element's style sheets
				var inheritedRules = selfRules.ToList();
				if (inheritableRules != null)
					inheritedRules.AddRange(inheritableRules);

				var potentiallyInheritedRules =
					inheritedRules.Where(r => element.MatchSelectorDistance(r.Selector) > 1).ToList();

				var potentiallyInheritedStyles = potentiallyInheritedRules
												 .SortByWeight()
												 .Select(r => r.Style).ToList();

				var potentiallyInheritedStyleObject = potentiallyInheritedStyles.MergeStyles();

				var finalInheritedStyleObject = inherit == null
					? potentiallyInheritedStyleObject
					: inherit.Merge(potentiallyInheritedStyleObject);

				// Add the rules into the next set, now that they've been processed for potential inheritence
				selfRules.AddRange(parentRules);

				// find and merge any styles from other rules that didn't apply to parent lineage, but do apply this this
				var directMatches = selfRules.Where(r => element.IsDirectMatch(r.Selector)).ToList();

				var styles = directMatches
							 .SortByWeight()
							 .Select(r => r.Style).ToList();

				var direct = styles.MergeStyles();

				var final = finalInheritedStyleObject == null
					? direct
					: finalInheritedStyleObject.Merge(direct);

				// set the styles!
				element.SetStyles(final, finalInheritedStyleObject, direct);

				// recursively call on all children elements.
				foreach (var child in element.GetChildren())
				{
					// always pass null for the inheritableRules, because we've already figured them out.
					Apply(child, final, selfRules, null);
				}
			}

			Apply(start, null, baseRules, baseInheritRules);
		}
	}
}
