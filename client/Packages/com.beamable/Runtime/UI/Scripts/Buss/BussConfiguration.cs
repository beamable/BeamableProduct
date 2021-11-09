using System.Collections.Generic;
using Beamable.UI.BUSS;
using UnityEngine;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Beamable.UI.Buss // TODO: rename it to Beamable.UI.BUSS - new system's namespace
{
    public class BussConfiguration : ModuleConfigurationObject
    {
        #region Old system

        public StyleSheetObject FallbackSheet;

        public List<StyleSheetObject> DefaultSheets = new List<StyleSheetObject>();

        public IEnumerable<StyleSheetObject> EnumerateSheets()
        {
            foreach (var sheet in DefaultSheets)
            {
                if (sheet != null)
                {
                    yield return sheet;
                }
            }

            if (FallbackSheet != null)
            {
                yield return FallbackSheet;
            }
        }

        #endregion

        // New system
        public static BussConfiguration Instance => Get<BussConfiguration>();
        [SerializeField] private BUSSStyleSheet globalStyleSheet = null;

        // TODO: serialized only for debug purposes. Remove before final push
        [SerializeField] private List<BUSSElement> _rootBussElements = new List<BUSSElement>();

        public void RegisterObserver(BUSSElement bussElement)
        {
            // TODO: serve case when user adds (by Add Component opiton, not by changing hierarchy) BUSSStyleProvider
            // component somewhere "above" currently topmost BUSSStyleProvider(s) causing to change whole hierarchy 
 
            if (!_rootBussElements.Contains(bussElement))
            {
                _rootBussElements.Add(bussElement);
            }
        }

        public void UnregisterObserver(BUSSElement bussElement)
        {
            _rootBussElements.Remove(bussElement);
        }

        public void UpdateStyleSheet(BUSSStyleSheet styleSheet) {
            // this should happen only in editor
            if (styleSheet == null) return;
            if (styleSheet == globalStyleSheet) {
                foreach (var bussElement in _rootBussElements) {
                    bussElement.OnStyleChanged();
                }
            }
            else {
                foreach (var bussElement in _rootBussElements) {
                    OnStyleSheetChanged(bussElement, styleSheet);
                }
            }
        }

        private void OnStyleSheetChanged(BUSSElement element, BUSSStyleSheet styleSheet) {
            if (element.StyleSheet == styleSheet) {
                element.OnStyleChanged();
            }
            else {
                foreach (var child in element.Children) {
                    OnStyleSheetChanged(child, styleSheet);
                }
            }
        }

        private void OnDestroy()
        {
            if (globalStyleSheet != null)
            {
                globalStyleSheet.OnChange -= OnGlobalStyleChanged;
            }
        }
        
        private void OnDisable()
        {
            if (globalStyleSheet != null)
            {
                globalStyleSheet.OnChange -= OnGlobalStyleChanged;
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

        public void RecalculateStyle(BUSSElement element) {
            element.Style.Clear();
            element.PseudoStyles.Clear();

            if (globalStyleSheet != null) {
                ApplyStyleSheet(element, globalStyleSheet);
            }

            foreach (var styleSheet in element.AllStyleSheets) {
                if (styleSheet != null) {
                    ApplyStyleSheet(element, styleSheet);
                }
            }
            
            ApplyDescriptor(element, element.InlineStyle);
            
            element.ApplyStyle();
        }

        public static void ApplyStyleSheet(BUSSElement element, BUSSStyleSheet sheet) {
            foreach (var descriptor in sheet.Styles) {
                if (descriptor.Selector?.CheckMatch(element) ?? false) {
                    ApplyDescriptor(element, descriptor);
                }
            }
        }

        public static void ApplyDescriptor(BUSSElement element, BUSSStyleDescription descriptor) {
            foreach (var property in descriptor.Properties) {
                element.Style[property.Key] = property.GetProperty();
            }
        }

        #endregion
    }
}