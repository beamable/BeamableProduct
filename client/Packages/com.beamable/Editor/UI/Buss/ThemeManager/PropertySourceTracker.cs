using System;
using System.Collections.Generic;
using System.Linq;
using PropertyReference = Beamable.UI.Buss.VariableDatabase.PropertyReference;

namespace Beamable.UI.Buss
{
	public class PropertySourceTracker
	{
		private readonly Dictionary<string, SourceData> _sources = new Dictionary<string, SourceData>();
		public BussElement Element { get; }

		public PropertySourceTracker(BussElement element)
		{
			Element = element;
			Recalculate();
		}

		public void Recalculate()
		{
			_sources.Clear();

			if (BussConfiguration.OptionalInstance.HasValue)
			{
				var config = BussConfiguration.OptionalInstance.Value;
				if (config != null)
				{
					foreach (BussStyleSheet styleSheet in config.DefaultBeamableStyleSheetSheets)
					{
						AddStyleSheet(styleSheet);
					}

					foreach (BussStyleSheet styleSheet in config.GlobalStyleSheets)
					{
						AddStyleSheet(styleSheet);
					}
				}
			}

			foreach (BussStyleSheet bussStyleSheet in Element.AllStyleSheets)
			{
				if (bussStyleSheet != null)
				{
					AddStyleSheet(bussStyleSheet);
				}
			}

			AddStyleDescription(null, Element.InlineStyle);
		}

		public bool IsUsed(string key, BussStyleRule styleRule)
		{
			var data = _sources[key];
			return data.Properties.First().StyleRule == styleRule;
		}

		public BussPropertyProvider GetUsedPropertyProvider(string key)
		{
			return GetUsedPropertyProvider(key, BussStyle.GetBaseType(key));
		}

		public BussPropertyProvider GetUsedPropertyProvider(string key, Type baseType)
		{
			if (_sources.ContainsKey(key))
			{
				foreach (var reference in _sources[key].Properties)
				{
					if (reference.PropertyProvider.IsPropertyOfType(baseType) || reference.PropertyProvider.IsPropertyOfType(typeof(VariableProperty)))
					{
						return reference.PropertyProvider;
					}
				}
			}

			return null;
		}

		public PropertyReference GetUsedPropertyReference(string key)
		{
			return GetUsedPropertyReference(key, BussStyle.GetBaseType(key));
		}

		public PropertyReference GetUsedPropertyReference(string key, Type baseType)
		{
			if (_sources.ContainsKey(key))
			{
				foreach (var reference in _sources[key].Properties)
				{
					if (reference.PropertyProvider.IsPropertyOfType(baseType) || reference.PropertyProvider.IsPropertyOfType(typeof(VariableProperty)))
					{
						return reference;
					}
				}
			}

			return new PropertyReference();
		}

		private void AddStyleSheet(BussStyleSheet styleSheet)
		{
			if (styleSheet == null) return;
			foreach (BussStyleRule styleRule in styleSheet.Styles)
			{
				if (styleRule.Selector?.CheckMatch(Element) ?? false)
				{
					AddStyleDescription(styleSheet, styleRule);
				}
			}
		}

		private void AddStyleDescription(BussStyleSheet styleSheet, BussStyleDescription styleDescription)
		{
			foreach (BussPropertyProvider property in styleDescription.Properties)
			{
				AddPropertySource(styleSheet, styleDescription as BussStyleRule, property);
			}
		}

		private void AddPropertySource(BussStyleSheet styleSheet, BussStyleRule styleRule, BussPropertyProvider propertyProvider)
		{
			var key = propertyProvider.Key;
			var propertyReference = new PropertyReference(styleSheet, styleRule, propertyProvider);
			if (!_sources.TryGetValue(key, out SourceData sourceData))
			{
				_sources[key] = sourceData = new SourceData(key);
			}
			sourceData.AddSource(propertyReference);
		}

		public class SourceData
		{
			public readonly string key;
			public readonly List<PropertyReference> Properties = new List<PropertyReference>();

			public SourceData(string key)
			{
				this.key = key;
			}

			public void AddSource(PropertyReference propertyReference)
			{
				var weight = propertyReference.GetWeight();
				var index = Properties.FindIndex(r => weight.CompareTo(r.GetWeight()) >= 0);
				if (index < 0)
				{
					Properties.Add(propertyReference);
				}
				else
				{
					Properties.Insert(index, propertyReference);
				}
			}
		}
	}

	public class PropertySourceDatabase
	{
		private readonly Dictionary<BussElement, PropertySourceTracker> _trackers = new Dictionary<BussElement, PropertySourceTracker>();

		public PropertySourceTracker GetTracker(BussElement bussElement)
		{
			if (bussElement == null) return null;

			if (!_trackers.TryGetValue(bussElement, out var tracker))
			{
				tracker = new PropertySourceTracker(bussElement);
				_trackers[bussElement] = tracker;
				bussElement.StyleRecalculated += tracker.Recalculate;
			}

			return tracker;
		}

		public void Discard()
		{
			foreach (KeyValuePair<BussElement, PropertySourceTracker> pair in _trackers)
			{
				if (pair.Key != null)
				{
					pair.Key.StyleRecalculated -= pair.Value.Recalculate;
				}
			}
			_trackers.Clear();
		}
	}
}
