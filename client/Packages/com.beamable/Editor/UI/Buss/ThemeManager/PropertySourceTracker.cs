using Beamable.UI.Buss;
using System.Collections.Generic;
using PropertyReference = Beamable.UI.Buss.VariableDatabase.PropertyReference;

namespace Beamable.UI.Buss
{
	public class PropertySourceTracker
	{
		public BussElement Element { get; }
		private Dictionary<string, SourceData> _sources = new Dictionary<string, SourceData>();

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
				if (config != null && config.GlobalStyleSheet != null)
				{
					AddStyleSheet(config.GlobalStyleSheet);
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
			return data.UsedProperty.styleRule == styleRule;
		}

		private void AddStyleSheet(BussStyleSheet styleSheet)
		{
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
			SourceData sourceData;
			if (!_sources.TryGetValue(key, out sourceData))
			{
				_sources[key] = sourceData = new SourceData(key);
			}
			sourceData.AddSource(propertyReference);
		}

		public class SourceData
		{
			public readonly string key;

			public SelectorWeight CurrentPropertyWeight = SelectorWeight.Min;
			public PropertyReference UsedProperty { get; private set; }
			public List<PropertyReference> OverridenProperties = new List<PropertyReference>();

			public SourceData(string key)
			{
				this.key = key;
			}

			public void AddSource(PropertyReference propertyReference)
			{
				var weight = propertyReference.GetWeight();
				if (weight.CompareTo(CurrentPropertyWeight) >= 0)
				{
					CurrentPropertyWeight = weight;
					OverridenProperties.Add(UsedProperty);
					UsedProperty = propertyReference;
				}
				else
				{
					OverridenProperties.Add(propertyReference);
				}
			}
		}
	}
}
