using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.UI.Buss
{
	public class VariableDatabase
	{
		private readonly Dictionary<string, VariableData> _variables = new Dictionary<string, VariableData>();
		private readonly HashSet<string> _variablesChecked = new HashSet<string>();
		private List<BussStyleSheet> _styleSheets = new List<BussStyleSheet>();

		public bool ForceRefreshAll { get; private set; }
		public HashSet<PropertyReference> DirtyProperties { get; } = new HashSet<PropertyReference>();

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
						GetVariableData(propertyProvider.Key).Declarations
						                                     .Add(new PropertyReference(sheet, rule, propertyProvider));
					}

					if (property is VariableProperty variableProperty &&
					    BussStyleSheetUtility.IsValidVariableName(variableProperty.VariableName))
					{
						GetVariableData(variableProperty.VariableName).Usages
						                                              .Add(
							                                              new PropertyReference(
								                                              sheet, rule, propertyProvider));
					}
				}
			}

			_styleSheets.Add(sheet);
		}

		public void RemoveStyleSheet(BussStyleSheet sheet)
		{
			foreach (VariableData variableData in _variables.Values)
			{
				variableData.Declarations.RemoveAll(pr => pr.StyleSheet == sheet);
				variableData.Usages.RemoveAll(pr => pr.StyleSheet == sheet);
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

		public void SetCrushingChange() => ForceRefreshAll = true;

		public void SetPropertyDirty(BussStyleSheet styleSheet,
		                             BussStyleRule styleRule,
		                             BussPropertyProvider propertyProvider)
		{
			DirtyProperties.Add(new PropertyReference(styleSheet, styleRule, propertyProvider));
		}

		public void SetVariableDirty(string key)
		{
			var data = GetVariableData(key);
			foreach (PropertyReference declaration in data.Declarations)
			{
				DirtyProperties.Add(declaration);
			}

			foreach (PropertyReference usage in data.Usages)
			{
				DirtyProperties.Add(usage);
			}
		}

		public void FlushDirtyMarkers()
		{
			ForceRefreshAll = false;
			DirtyProperties.Clear();
		}

		public void ResetVariableLoopDetector() => _variablesChecked.Clear();

		public PropertyValueState TryGetVariableValue(VariableProperty variableProperty,
		                                              BussStyleRule styleRule,
		                                              out IBussProperty result,
		                                              BussElement context,
		                                              Type expectedType)
		{
			if (expectedType == null)
			{
				expectedType = typeof(IBussProperty);
			}

			result = null;
			var variableName = variableProperty.VariableName;

			if (_variablesChecked.Contains(variableName))
			{
				return PropertyValueState.VariableLoopDetected;
			}

			_variablesChecked.Add(variableProperty.VariableName);

			//TODO: add different behaviour when context exists
			return TryGetVariableValueWithoutContext(variableProperty, styleRule, out result, expectedType);
		}

		private PropertyValueState TryGetVariableValueWithoutContext(VariableProperty variableProperty,
		                                                             BussStyleRule styleRule,
		                                                             out IBussProperty result,
		                                                             Type expectedType)
		{
			result = null;
			if (styleRule.HasProperty(variableProperty.VariableName))
			{
				result = styleRule.GetProperty(variableProperty.VariableName);
				return PropertyValueState.SingleResult;
			}

			var variableData = GetVariableData(variableProperty.VariableName);
			var declarations = variableData.Declarations.Where(
				r => expectedType.IsInstanceOfType(r.PropertyProvider.GetProperty()));
			var declarationsCount = declarations.Count();
			if (declarationsCount == 1)
			{
				result = declarations.First().PropertyProvider.GetProperty();
				return PropertyValueState.SingleResult;
			}

			if (declarationsCount == 0)
			{
				return PropertyValueState.NoResult;
			}

			return PropertyValueState.MultipleResults;
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
				return Declarations.Where(pr => pr.StyleSheet == sheet);
			}

			public PropertyReference GetDeclarationsFrom(BussStyleRule rule)
			{
				return Declarations.FirstOrDefault(pr => pr.StyleRule == rule);
			}

			public IEnumerable<PropertyReference> GetUsagesFrom(BussStyleSheet sheet)
			{
				return Usages.Where(pr => pr.StyleSheet == sheet);
			}

			public PropertyReference GetUsageFrom(BussStyleRule rule)
			{
				return Usages.FirstOrDefault(pr => pr.StyleRule == rule);
			}

			public bool HasTypeDeclared(Type type)
			{
				return Declarations.Any(pr => type.IsInstanceOfType(pr.PropertyProvider.GetProperty()));
			}
		}

		public struct PropertyReference
		{
			public BussStyleSheet StyleSheet;
			public BussStyleRule StyleRule;
			public BussPropertyProvider PropertyProvider;

			public PropertyReference(BussStyleSheet styleSheet,
			                         BussStyleRule styleRule,
			                         BussPropertyProvider propertyProvider)
			{
				StyleSheet = styleSheet;
				StyleRule = styleRule;
				PropertyProvider = propertyProvider;
			}

			public SelectorWeight GetWeight()
			{
				return StyleRule == null ? SelectorWeight.Max : StyleRule.Selector.GetWeight();
			}
		}

		public enum PropertyValueState
		{
			NoResult,
			SingleResult,
			MultipleResults,
			VariableLoopDetected
		}
	}
}
