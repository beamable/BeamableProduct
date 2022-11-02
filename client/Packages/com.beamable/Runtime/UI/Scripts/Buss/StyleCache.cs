using System.Collections.Generic;
using System.Linq;

namespace Beamable.UI.Buss
{
	public class StyleCache
	{

		private static StyleCache _instance;
		public static StyleCache Instance => _instance ?? (_instance = new StyleCache());


		private Dictionary<BussStyleRule, HashSet<BussElement>> _ruleToElements =
			new Dictionary<BussStyleRule, HashSet<BussElement>>();

		private Dictionary<BussStyleRule, Dictionary<string, HashSet<BussElement>>> _ruleToKeyToElements =
			new Dictionary<BussStyleRule, Dictionary<string, HashSet<BussElement>>>();

		private Dictionary<BussElement, HashSet<BussStyleRule>> _elementToRules =
			new Dictionary<BussElement, HashSet<BussStyleRule>>();

		private StyleCache()
		{
			// private constructor
		}
		
		public IEnumerable<BussElement> GetElementsReferencingRule(BussStyleRule rule, string key)
		{
			if (!_ruleToKeyToElements.TryGetValue(rule, out var keyedElements))
			{
				yield break;
			}

			if (!keyedElements.TryGetValue(key, out var elements))
			{
				yield break;
			}

			foreach (var elem in elements.ToList())
			{
				yield return elem;
			}
		}
		
		public StyleCacheEntry AttachReference(BussElement element, string key, PropertyReference reference)
		{
			if (_ruleToElements.TryGetValue(reference.StyleRule, out var elements))
			{
				elements.Add(element);
			}
			else
			{
				_ruleToElements.Add(reference.StyleRule, new HashSet<BussElement>{element});
			}

			if (_ruleToKeyToElements.TryGetValue(reference.StyleRule, out var keyedElements))
			{
				if (keyedElements.TryGetValue(key, out elements))
				{
					elements.Add(element);
				}
				else
				{
					keyedElements.Add(key, new HashSet<BussElement>{element});
				}
			}
			else
			{
				_ruleToKeyToElements.Add(reference.StyleRule, new Dictionary<string, HashSet<BussElement>>
				{
					[key] = new HashSet<BussElement>{element}
				});
			}

			return new StyleCacheEntry(element, key, reference.StyleRule, this);
		}

		public void Clear(BussElement element)
		{
			// need to identify all the 
			if (_elementToRules.TryGetValue(element, out var rules))
			{
				
			}
		}
		
		public class StyleCacheEntry
		{
			public BussElement Element { get; }
			public string Key { get; }
			public BussStyleRule Rule { get; }
			public StyleCache Cache { get; }

			public StyleCacheEntry(BussElement element, string key, BussStyleRule rule, StyleCache cache)
			{
				Element = element;
				Key = key;
				Rule = rule;
				Cache = cache;
			}
		
			public void Release()
			{
				if (Cache._ruleToElements.TryGetValue(Rule, out var elements))
				{
					elements.Remove(Element);
				}

				if (Cache._ruleToKeyToElements.TryGetValue(Rule, out var keyedElements))
				{
					if (keyedElements.TryGetValue(Key, out elements))
					{
						elements.Remove(Element);
					}
				}
			}
		}
	}
}
