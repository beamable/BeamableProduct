using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using PropertyReference = Beamable.UI.Buss.PropertyReference;

namespace Beamable.UI.Buss
{
	public interface IVariableNameProvider
	{
		PropertyReference ResolveVariableProperty(string key);
		IEnumerable<string> GetAllVariableNamesNonLooping(BussPropertyProvider provider, Type baseType);
	}
	
	
	public class PropertySourceTracker : IVariableNameProvider
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
					foreach (BussStyleSheet styleSheet in config.FactoryStyleSheets)
					{
						AddStyleSheet(styleSheet);
					}

					foreach (BussStyleSheet styleSheet in config.DeveloperStyleSheets)
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

		public IEnumerable<string> GetKeys() => _sources.Keys;

		/// <summary>
		/// Enumerate all <see cref="PropertyReference"/> objects of the given key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public IEnumerable<PropertyReference> GetAllSources(string key)
		{
			if (!_sources.TryGetValue(key, out var sourceData))
			{
				yield break;
			}

			foreach (var reference in sourceData.Properties)
			{
				yield return reference;
			}
		}

		/// <summary>
		/// Get all the variable names in the given style that don't lead back to an infinite loop 
		/// </summary>
		/// <param name="provider">The starting property</param>
		/// <param name="baseType">The type of variable to look for.</param>
		/// <returns></returns>
		public IEnumerable<string> GetAllVariableNamesNonLooping(BussPropertyProvider provider, Type baseType)
		{
			var startingName = provider.Key;
			var referenceMap = new Dictionary<string, HashSet<string>>();
			var candidates = new HashSet<string>();
			
			// phase 1, build up a map of references and variable name candidates.
			//          we can't check for loops in this first pass, because its possible not all the
			//          references have been identified.
			foreach (var kvp in _sources)
			{
				if (!BussStyleSheetUtility.IsValidVariableName(kvp.Key)) continue;
				var firstProperty = kvp.Value.Properties.FirstOrDefault();
				if (firstProperty == null) continue;
				
				if (!firstProperty.PropertyProvider.IsPropertyOfType(baseType)) continue;

				var prop = firstProperty.PropertyProvider.GetProperty();
				if (!referenceMap.TryGetValue(kvp.Key, out var references))
				{
					referenceMap[kvp.Key] = references = new HashSet<string>();
				}

				if (prop is VariableProperty variableProperty)
				{
					references.Add(variableProperty.VariableName);
				} else if (prop is IComputedProperty computedProperty)
				{
					foreach (var member in computedProperty.Members)
					{
						if (member.Property is VariableProperty computedPropertyVariableProperty)
						{
							references.Add(computedPropertyVariableProperty.VariableName);
						}
					}
				}
				
				candidates.Add(kvp.Key);
			}

			// Return true if the given key would result in an asyclic reference to the start
			bool HasCycle(string start, string key)
			{
				bool Iteration(string curr, int maxIter, HashSet<string> seen)
				{
					if (maxIter <= 0) return true; // infinite-loop base case: Sure, the editor will be laggy, but it shouldn't brick.
					if (seen.Contains(curr)) return true; // base case: We've detected a loop!

					seen.Add(curr); // remember this node
					
					if (!referenceMap.TryGetValue(curr, out var references))
					{
						return false; // there are no more references from this location, so if we haven't looped yet, we can't
					}

					foreach (var reference in references)
					{
						if (Iteration(reference, maxIter - 1, new HashSet<string>(seen)))
						{
							return true; // the recursive call resulted in a loop, so by nature, this call contains a loop too.
						}
					}

					return false; // we didn't find any loops
				}

				return Iteration(key, 100, new HashSet<string>{start});
			}
			
			// phase 2, check the candidates against the reference map.
			foreach (var key in candidates)
			{
				if (HasCycle(startingName, key)) continue;
				yield return key;
			}
		}

		/// <summary>
		/// This method should accept a <see cref="BussPropertyProvider"/> that has an inherited value type.
		/// Given an inherited property provider, this will find the effective provider beyond the given property.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public BussPropertyProvider GetNextInheritedProperty(BussPropertyProvider property)
		{
			if (property.ValueType != BussPropertyValueType.Inherited) return property;

			var found = false;
			foreach (var reference in GetAllSources(property.Key))
			{
				if (found)
				{
					if (reference.PropertyProvider.ValueType == BussPropertyValueType.Inherited) continue;
					var inheritedReference = reference.PropertyProvider;

					if (inheritedReference.HasVariableReference)
					{
						var variableProperty = inheritedReference.GetProperty() as VariableProperty;
						var variableName = variableProperty.VariableName;

						var referenceValue = GetUsedPropertyProvider(variableName, out _);
						return referenceValue;
					}
					else
					{
						return reference.PropertyProvider;
					}

				}
				if (reference.PropertyProvider == property)
				{
					found = true;
				}
			}

			return null;
		}

		public BussPropertyProvider GetUsedPropertyProvider(string key, out int rank)
		{
			return GetUsedPropertyProvider(key, BussStyle.GetBaseType(key), false, out rank).Item1;
		}

		public PropertyReference ResolveVariableProperty(string key)
		{
			return GetUsedPropertyProvider(key, BussStyle.GetBaseType(key), true, out _).Item2;
		}

		public (BussPropertyProvider, PropertyReference) GetUsedPropertyProvider(string key, Type baseType, bool resolveVariables, out int rank)
		{
			rank = 0;
			if (_sources.ContainsKey(key))
			{
				var requestedInheritence = false;
				foreach (var reference in _sources[key].Properties)
				{
					rank++;
					// if the reference says, "inherit", then the used property should continue up the reference sequence
					if (reference.PropertyProvider.ValueType == BussPropertyValueType.Inherited)
					{
						requestedInheritence = true;
						continue;
					}

					// if the reference is a variable, redirect!
					if (resolveVariables && reference.PropertyProvider.HasVariableReference)
					{
						var variableProperty = reference.PropertyProvider.GetProperty() as VariableProperty;
						var variableName = variableProperty.VariableName;

						var referenceValue = GetUsedPropertyProvider(variableName, baseType, true, out var nestedRank);
						return (referenceValue.Item1, referenceValue.Item2);
					}

					if (reference.PropertyProvider.IsPropertyOfType(baseType) ||
						reference.PropertyProvider.IsPropertyOfType(typeof(VariableProperty)))
					{

						if (!requestedInheritence)
						{
							// if this is the first source, it means we haven't necessarily asked to inherit the rule.
							if (BussStyle.TryGetBinding(key, out var binding) && !binding.Inheritable)
							{
								// if we aren't an exact match, we need to continue...
								if ((!reference.StyleRule?.Selector.CheckMatch(Element)) ?? false)
								{
									continue;
								}
							}
						}
						
						return (reference.PropertyProvider, reference);
					}
				}
			}

			return (null, null);
		}

		public PropertyReference GetUsedPropertyReference(string key)
		{
			return GetUsedPropertyReference(key, BussStyle.GetBaseType(key));
		}

		private PropertyReference GetUsedPropertyReference(string key, Type baseType)
		{
			if (_sources.ContainsKey(key))
			{
				foreach (var reference in _sources[key].Properties)
				{
					if (reference.PropertyProvider.IsPropertyOfType(baseType) ||
						reference.PropertyProvider.IsPropertyOfType(typeof(VariableProperty)))
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
				if (styleRule.Selector?.IsElementIncludedInSelector(Element) ?? false)
				{
					AddStyleDescription(styleSheet, styleRule);
				}
			}
		}

		private void AddStyleDescription(BussStyleSheet styleSheet, BussStyleDescription styleDescription)
		{
			if (styleDescription == null || styleDescription.Properties == null) return;
			foreach (BussPropertyProvider property in styleDescription.Properties)
			{
				AddPropertySource(styleSheet, styleDescription as BussStyleRule, property);
			}
		}

		private void AddPropertySource(BussStyleSheet styleSheet,
									   BussStyleRule styleRule,
									   BussPropertyProvider propertyProvider)
		{
			var key = propertyProvider.Key;

			var exactMatch = true;
			if (styleRule != null)
			{
				if (!styleRule.Selector.IsElementIncludedInSelector(Element, out exactMatch))
				{
					// this is an inherited property, but maybe the property isn't inheritable?
					if (BussStyle.TryGetBinding(key, out var binding) && !binding.Inheritable) return;
				}
			}


			var propertyReference = new PropertyReference(key, styleSheet, styleRule, propertyProvider);
			if (!_sources.TryGetValue(key, out SourceData sourceData))
			{
				_sources[key] = sourceData = new SourceData(key);
			}

			sourceData.AddSource(propertyReference, exactMatch);
		}

		public class SourceData
		{
			public readonly string key;
			public readonly List<PropertyReference> Properties = new List<PropertyReference>();


			private readonly List<PropertyReference> InheritedProperties = new List<PropertyReference>();
			private readonly List<PropertyReference> MatchedProperties = new List<PropertyReference>();

			public SourceData(string key)
			{
				this.key = key;
			}

			public void AddSource(PropertyReference propertyReference, bool exactMatch)
			{
				var propList = MatchedProperties;
				if (!exactMatch)
				{
					propList = InheritedProperties;
				}

				var weight = propertyReference.GetWeight();
				var index = propList.FindIndex(r => weight.CompareTo(r.GetWeight()) >= 0);
				if (index < 0)
				{
					propList.Add(propertyReference);
				}
				else
				{
					propList.Insert(index, propertyReference);
				}

				Properties.Clear();
				Properties.AddRange(MatchedProperties);
				Properties.AddRange(InheritedProperties);
			}
		}

	}

	public class PropertySourceDatabase
	{
		private readonly Dictionary<BussElement, PropertySourceTracker> _trackers =
			new Dictionary<BussElement, PropertySourceTracker>();

		public PropertySourceTracker GetTracker(BussElement bussElement)
		{
			if (bussElement == null) return null;
			var tracker = bussElement.Sources;
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
