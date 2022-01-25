using Beamable.Common.Content;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Beamable.UI.Buss
{
	public class BussConfiguration : ModuleConfigurationObject
	{
#pragma warning disable CS0649
		// TODO: add release date to track name change timestamp
		[FormerlySerializedAs("globalStyleSheet")] [SerializeField] private BussStyleSheet _globalStyleSheet = null;
#pragma warning restore CS0649

		private readonly List<BussElement> _rootBussElements = new List<BussElement>();

		private static BussConfiguration Instance => Get<BussConfiguration>();

		private static Optional<BussConfiguration> OptionalInstance
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

		public static void UseConfig(Action<BussConfiguration> callback)
		{
			OptionalInstance.DoIfExists(callback);
		}

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
		}
#endif

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

			ApplyDescriptor(element, element.InlineStyle);

			element.ApplyStyle();
		}

		private static void ApplyStyleSheet(BussElement element, BussStyleSheet sheet)
		{
			if (element == null || sheet == null) return;
			foreach (var descriptor in sheet.Styles)
			{
				if (descriptor.Selector?.CheckMatch(element) ?? false)
				{
					if (descriptor.Selector.TryGetPseudoClass(out var pseudoClass))
					{
						ApplyDescriptorWithPseudoClass(element, pseudoClass, descriptor);
					}
					else
					{
						ApplyDescriptor(element, descriptor);
					}
				}
			}
		}

		private static void ApplyDescriptor(BussElement element, BussStyleDescription descriptor)
		{
			if (element == null || descriptor == null) return;
			foreach (var property in descriptor.Properties)
			{
				element.Style[property.Key] = property.GetProperty();
			}
		}

		private static void ApplyDescriptorWithPseudoClass(BussElement element,
		                                                   string pseudoClass,
		                                                   BussStyleDescription descriptor)
		{
			if (element == null || descriptor == null) return;
			foreach (var property in descriptor.Properties)
			{
				element.Style[pseudoClass, property.Key] = property.GetProperty();
			}
		}

		#endregion
	}
}
