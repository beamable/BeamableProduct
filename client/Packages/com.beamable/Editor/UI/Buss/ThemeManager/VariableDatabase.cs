using System.Collections.Generic;
using System.Linq;
using Beamable.UI.Buss;

namespace Editor.UI.BUSS.ThemeManager
{
	public class VariableDatabase
	{
		private List<BussStyleSheet> _styleSheets = new List<BussStyleSheet>();
		private Dictionary<string, VariableData> _variables = new Dictionary<string, VariableData>();

		public VariableData GetVariableData(string key)
		{
			if (_variables.TryGetValue(key, out var value))
			{
				return value;
			}
			var data = new VariableData(key);
			_variables[key] = data;
			return data;
		}

		public IEnumerable<string> GetVariableNames()
		{
			return _variables.Keys;
		}
		
		public void AddStyleSheet(BussStyleSheet sheet)
		{
			if (sheet == null) return;
			
			if (_styleSheets.Contains(sheet))
			{
				RemoveStyleSheet(sheet);
			}
			
			foreach (BussStyleRule rule in sheet.Styles)
			{
				foreach (BussPropertyProvider propertyProvider in rule.Properties)
				{
					var property = propertyProvider.GetProperty();
					if (BussStyleSheetUtility.IsValidVariableName(propertyProvider.Key))
					{
						GetVariableData(propertyProvider.Key).Declarations.Add(new PropertyReference(sheet, rule, propertyProvider));
					}

					if (property is VariableProperty variableProperty && BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
					{
						GetVariableData(variableProperty.VariableName).Usages.Add(new PropertyReference(sheet, rule, propertyProvider));
					}
				}
			}
			
			_styleSheets.Add(sheet);
		}

		public void RemoveStyleSheet(BussStyleSheet sheet)
		{
			foreach (VariableData variableData in _variables.Values)
			{
				variableData.Declarations.RemoveAll(pr => pr.styleSheet == sheet);
				variableData.Usages.RemoveAll(pr => pr.styleSheet == sheet);
			}
			
			_styleSheets.Remove(sheet);
		}

		public void RemoveAllStyleSheets()
		{
			_variables.Clear();
			_styleSheets.Clear();
		}

		public void ReconsiderStyleSheet(BussStyleSheet sheet)
		{
			RemoveStyleSheet(sheet);
			AddStyleSheet(sheet);
		}

		public void ReconsiderAllStyleSheets()
		{
			_variables.Clear();
			var styleSheets = _styleSheets;
			_styleSheets = new List<BussStyleSheet>();
			foreach (BussStyleSheet sheet in styleSheets)
			{
				AddStyleSheet(sheet);
			}
		}

		public class VariableData
		{
			public readonly string Name;
			public List<PropertyReference> Declarations = new List<PropertyReference>();
			public List<PropertyReference> Usages = new List<PropertyReference>();
			
			public VariableData(string name)
			{
				Name = name;
			}

			public IEnumerable<PropertyReference> GetDeclarationsFrom(BussStyleSheet sheet)
			{
				return Declarations.Where(pr => pr.styleSheet == sheet);
			}

			public PropertyReference GetDeclarationsFrom(BussStyleRule rule)
			{
				return Declarations.FirstOrDefault(pr => pr.styleRule == rule);
			}

			public IEnumerable<PropertyReference> GetUsagesFrom(BussStyleSheet sheet)
			{
				return Usages.Where(pr => pr.styleSheet == sheet);
			}

			public PropertyReference GetUsageFrom(BussStyleRule rule)
			{
				return Usages.FirstOrDefault(pr => pr.styleRule == rule);
			}
		}

		public class PropertyReference
		{
			public BussStyleSheet styleSheet;
			public BussStyleRule styleRule;
			public BussPropertyProvider propertyProvider;
			public PropertyReference(BussStyleSheet styleSheet, BussStyleRule styleRule, BussPropertyProvider propertyProvider)
			{
				this.styleSheet = styleSheet;
				this.styleRule = styleRule;
				this.propertyProvider = propertyProvider;
			}
		}
	}
}
