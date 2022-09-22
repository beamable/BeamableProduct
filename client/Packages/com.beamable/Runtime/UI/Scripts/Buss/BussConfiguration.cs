using Beamable.Common;
using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Beamable.UI.Buss
{
	public class BussConfiguration : ModuleConfigurationObject, IVariablesProvider
	{
		private static BussConfiguration Instance => Get<BussConfiguration>();

		public static Optional<BussConfiguration> OptionalInstance
		{
			get
			{
				try
				{
					return new Optional<BussConfiguration> { Value = Instance, HasValue = true };
				}
				catch (ModuleConfigurationNotReadyException)
				{
					return new Optional<BussConfiguration>();
				}
			}
		}

		private static readonly Dictionary<string, SelectorWeight> Weights = new Dictionary<string, SelectorWeight>();

		public static void UseConfig(Action<BussConfiguration> callback)
		{
			OptionalInstance.DoIfExists(callback);
		}

		[SerializeField] private List<BussStyleSheet> _globalStyleSheets = new List<BussStyleSheet>();

		private readonly List<BussStyleSheet> _defaultBeamableStyleSheets = new List<BussStyleSheet>();
		private readonly List<BussElement> _rootBussElements = new List<BussElement>();

		public List<BussStyleSheet> DefaultBeamableStyleSheetSheets => _defaultBeamableStyleSheets;
		public List<BussStyleSheet> GlobalStyleSheets => _globalStyleSheets;
		public List<BussElement> RootBussElements => _rootBussElements;
		public List<BussStyleSheet> StyleSheets { get; set; }

		// private VariableDatabase _variableDatabase;

#if UNITY_EDITOR
		static BussConfiguration()
		{
			// temporary solution to refresh the list of BussElements on scene change
			EditorSceneManager.sceneOpened += (scene, mode) => UseConfig(config => config.RefreshBussElements());
			EditorSceneManager.sceneClosed += scene => UseConfig(config => config.RefreshBussElements());
		}

		void RefreshBussElements()
		{
			_rootBussElements.Clear();
			foreach (BussElement element in FindObjectsOfType<BussElement>())
			{
				element.CheckParent();
			}

			EditorUtility.SetDirty(this);
			
			RefreshDefaultStyles();
		}
#endif

		public void AddGlobalStyleSheet(BussStyleSheet styleSheet)
		{
			if (!GlobalStyleSheets.Contains(styleSheet))
			{
				GlobalStyleSheets.Add(styleSheet);
				UpdateStyleSheet(styleSheet);
			}
		}

		public void RegisterObserver(BussElement bussElement)
		{
			// TODO: serve case when user adds (by Add Component option, not by changing hierarchy) BUSSStyleProvider
			// component somewhere "above" currently topmost BUSSStyleProvider(s) causing to change whole hierarchy

			if (!_rootBussElements.Contains(bussElement))
			{
				_rootBussElements.Add(bussElement);
			}
		}

		public void UnregisterObserver(BussElement bussElement)
		{
			_rootBussElements.Remove(bussElement);
		}

		public void UpdateStyleSheet(BussStyleSheet styleSheet)
		{
			// This should happen only in editor
			if (styleSheet == null) return;

			RefreshDefaultStyles();

			if (_defaultBeamableStyleSheets.Contains(styleSheet) || _globalStyleSheets.Contains(styleSheet))
			{
				foreach (BussElement bussElement in _rootBussElements)
				{
					bussElement.OnStyleChanged();
				}
			}
			else
			{
				foreach (BussElement bussElement in _rootBussElements)
				{
					OnStyleSheetChanged(bussElement, styleSheet);
				}
			}
		}

		public void RefreshDefaultStyles()
		{
			_defaultBeamableStyleSheets.Clear();
			BussStyleSheet[] bussStyleSheets = Resources
											   .LoadAll<BussStyleSheet>(
												   Constants.Features.Buss.Paths.FACTORY_STYLES_RESOURCES_PATH)
											   .Where(styleSheet => styleSheet.IsReadOnly).ToArray();

			var orderedStyleSheets = bussStyleSheets.OrderBy(s => s.SortingOrder);

			_defaultBeamableStyleSheets.AddRange(orderedStyleSheets);
		}

		private void OnStyleSheetChanged(BussElement element, BussStyleSheet styleSheet)
		{
			if (element == null) return;
			if (element.StyleSheet == styleSheet)
			{
				element.OnStyleChanged();
			}
			else
			{
				foreach (BussElement child in element.Children)
				{
					OnStyleSheetChanged(child, styleSheet);
				}
			}
		}

		#region Styles parsing

		public void RecalculateStyle(BussElement element)
		{
			Weights.Clear();
			element.Style.Clear();

			// Applying default bemable styles
			foreach (BussStyleSheet styleSheet in _defaultBeamableStyleSheets)
			{
				ApplyStyleSheet(element, styleSheet);
			}

			// Applying developer styles
			foreach (BussStyleSheet styleSheet in _globalStyleSheets)
			{
				ApplyStyleSheet(element, styleSheet);
			}

			StyleSheets = new List<BussStyleSheet>(element.AllStyleSheets);

			foreach (BussStyleSheet styleSheet in element.AllStyleSheets)
			{
				if (styleSheet != null)
				{
					ApplyStyleSheet(element, styleSheet);
				}
			}

			ApplyDescriptor(element, element.InlineStyle, SelectorWeight.Max);

			element.ApplyStyle();
		}

		private static void ApplyStyleSheet(BussElement element, BussStyleSheet sheet)
		{
			if (element == null || sheet == null) return;
			foreach (BussStyleRule descriptor in sheet.Styles)
			{
				if (descriptor.Selector?.CheckMatch(element) ?? false)
				{
					SelectorWeight weight = descriptor.Selector.GetWeight();
					if (descriptor.Selector.TryGetPseudoClass(out string pseudoClass))
					{
						ApplyDescriptorWithPseudoClass(element, pseudoClass, descriptor, weight);
					}
					else
					{
						ApplyDescriptor(element, descriptor, weight);
					}
				}
			}
		}

		private static void ApplyDescriptor(BussElement element, BussStyleDescription descriptor, SelectorWeight weight)
		{
			if (element == null || descriptor == null) return;
			foreach (BussPropertyProvider property in descriptor.Properties)
			{
				if (!Weights.TryGetValue(property.Key, out SelectorWeight currentWeight) ||
					weight.CompareTo(currentWeight) >= 0)
				{
					element.Style[property.Key] = property.GetProperty();
					Weights[property.Key] = weight;
				}
			}
		}

		private static void ApplyDescriptorWithPseudoClass(BussElement element,
														   string pseudoClass,
														   BussStyleDescription descriptor,
														   SelectorWeight weight)
		{
			if (element == null || descriptor == null) return;
			foreach (BussPropertyProvider property in descriptor.Properties)
			{
				string weightKey = pseudoClass + property.Key;
				if (!Weights.TryGetValue(weightKey, out SelectorWeight currentWeight) ||
					weight.CompareTo(currentWeight) >= 0)
				{
					element.Style[pseudoClass, property.Key] = property.GetProperty();
					Weights[weightKey] = weight;
				}
			}
		}

		#endregion
	}
}
