using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using Beamable.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using Object = System.Object;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Beamable.UI.Buss // TODO: rename it to Beamable.UI.BUSS - new system's namespace
{
	public class BussConfiguration : ModuleConfigurationObject
	{
		[SerializeField] private BussStyleSheet _globalStyleSheet = null;

		private static Dictionary<string, SelectorWeight> _weights = new Dictionary<string, SelectorWeight>();

		private List<BussElement> _rootBussElements = new List<BussElement>();

		public static Optional<BussConfiguration> OptionalInstance
		{
			get
			{
				try
				{
					return new Optional<BussConfiguration> {Value = Instance, HasValue = true};
				}
				catch (ModuleConfigurationNotReadyException)
				{
					return new Optional<BussConfiguration>();
				}
			}
		}

		private static BussConfiguration Instance => Get<BussConfiguration>();

		public List<BussElement> RootBussElements => _rootBussElements;
		public BussStyleSheet GlobalStyleSheet => _globalStyleSheet;

#if UNITY_EDITOR
		static BussConfiguration()
		{
			// temporary solution to refresh the list of BussElements on scene change
			EditorSceneManager.sceneOpened += (scene, mode) => UseConfig(config => config.RefreshBussElements());
			EditorSceneManager.sceneClosed += scene => UseConfig(config => config.RefreshBussElements());

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
		}

		public override void OnFreshCopy()
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				
				BussStyleSheet globalStyleSheet = BussStyleSheetUtility.CreateGlobalStyleSheet();

				BussStyleSheet defaultGlobalStyleSheet =
					AssetDatabase.LoadAssetAtPath<BussStyleSheet>(
						Constants.Features.Buss.DEFAULT_GLOBAL_STYLE_SHEET_PATH);

				BussStyleSheetUtility.CopyStyles(defaultGlobalStyleSheet, globalStyleSheet);

				_globalStyleSheet = globalStyleSheet;
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				AssetDatabase.SaveAssets();
			}
		}

		private static void OnHierarchyChanged()
		{
			List<BeamableModule> prefabs = FindObjectsOfType<BeamableModule>().ToList();

			foreach (BeamableModule prefab in prefabs)
			{
				BussStyleSheetUtility.ValidateStyleSheets(prefab.gameObject);
			}
		}

		void RefreshBussElements()
		{
			_rootBussElements.Clear();
			foreach (BussElement element in FindObjectsOfType<BussElement>())
			{
				element.CheckParent();
			}

			EditorUtility.SetDirty(this);
		}

		public void SetGlobalStyleSheet(BussStyleSheet bussStyleSheet)
		{
			_globalStyleSheet = bussStyleSheet;
		}
#endif

		public static void UseConfig(Action<BussConfiguration> callback)
		{
			OptionalInstance.DoIfExists(callback);
		}

		public void RegisterObserver(BussElement bussElement)
		{
			// TODO: serve case when user adds (by Add Component opiton, not by changing hierarchy) BUSSStyleProvider
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
			// this should happen only in editor
			if (styleSheet == null) return;
			if (styleSheet == _globalStyleSheet)
			{
				foreach (var bussElement in _rootBussElements)
				{
					bussElement.OnStyleChanged();
				}
			}
			else
			{
				foreach (var bussElement in _rootBussElements)
				{
					OnStyleSheetChanged(bussElement, styleSheet);
				}
			}
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
				foreach (var child in element.Children)
				{
					OnStyleSheetChanged(child, styleSheet);
				}
			}
		}

		private void OnDestroy()
		{
			if (_globalStyleSheet != null)
			{
				_globalStyleSheet.Change -= OnGlobalStyleChanged;
			}
		}

		private void OnDisable()
		{
			if (_globalStyleSheet != null)
			{
				_globalStyleSheet.Change -= OnGlobalStyleChanged;
			}
		}

		private void OnGlobalStyleChanged()
		{
			foreach (var bussElement in _rootBussElements)
			{
				bussElement.OnStyleChanged();
			}
		}

		// TODO: in future move to some styles repository class which responsibility will be caching styles and recalculate them

		#region Styles parsing

		public void RecalculateStyle(BussElement element)
		{
			_weights.Clear();
			element.Style.Clear();
			element.PseudoStyles.Clear();

			if (_globalStyleSheet != null)
			{
				ApplyStyleSheet(element, _globalStyleSheet);
			}

			foreach (var styleSheet in element.AllStyleSheets)
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
			foreach (var descriptor in sheet.Styles)
			{
				if (descriptor.Selector?.CheckMatch(element) ?? false)
				{
					var weight = descriptor.Selector.GetWeight();
					if (descriptor.Selector.TryGetPseudoClass(out var pseudoClass))
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
			foreach (var property in descriptor.Properties)
			{
				if (!_weights.TryGetValue(property.Key, out var currentWeight) || weight.CompareTo(currentWeight) >= 0)
				{
					element.Style[property.Key] = property.GetProperty();
					_weights[property.Key] = weight;
				}
			}
		}

		private static void ApplyDescriptorWithPseudoClass(BussElement element,
		                                                   string pseudoClass,
		                                                   BussStyleDescription descriptor,
		                                                   SelectorWeight weight)
		{
			if (element == null || descriptor == null) return;
			foreach (var property in descriptor.Properties)
			{
				var weightKey = pseudoClass + property.Key;
				if (!_weights.TryGetValue(weightKey, out var currentWeight) || weight.CompareTo(currentWeight) >= 0)
				{
					element.Style[pseudoClass, property.Key] = property.GetProperty();
					_weights[weightKey] = weight;
				}
			}
		}

		#endregion
	}
}
