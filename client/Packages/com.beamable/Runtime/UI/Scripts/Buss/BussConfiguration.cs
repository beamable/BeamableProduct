using System.Collections.Generic;
using Beamable.UI.BUSS;
using UnityEngine;

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
        [SerializeField] private BUSSStyleConfig _globalStyleConfig;

        // TODO: serialized only for debug purposes. Remove before final push
        [SerializeField] private List<BUSSStyleProvider> _styleProviders = new List<BUSSStyleProvider>();

        public void RegisterObserver(BUSSStyleProvider styleProvider)
        {
            // TODO: serve case when user adds (by Add Component opiton, not by changing hierarchy) BUSSStyleProvider
            // component somewhere "above" currently topmost BUSSStyleProvider(s) causing to change whole hierarchy 

            if (!_styleProviders.Contains(styleProvider))
            {
                _styleProviders.Add(styleProvider);
            }
        }

        public void UnregisterObserver(BUSSStyleProvider styleProvider)
        {
            if (_styleProviders.Contains(styleProvider))
            {
                _styleProviders.Remove(styleProvider);
            }
        }

        private void OnValidate()
        {
            if (_globalStyleConfig != null)
            {
                _globalStyleConfig.OnChange += OnGlobalStyleChanged;
            }
        }

        private void OnDestroy()
        {
            if (_globalStyleConfig != null)
            {
                _globalStyleConfig.OnChange -= OnGlobalStyleChanged;
            }
        }
        
        private void OnDisable()
        {
            if (_globalStyleConfig != null)
            {
                _globalStyleConfig.OnChange -= OnGlobalStyleChanged;
            }
        }

        private void OnGlobalStyleChanged()
        {
            foreach (BUSSStyleProvider styleProvider in _styleProviders)
            {
                styleProvider.OnStyleChanged();
            }
        }

        // TODO: in future move to some styles repository class which responsibility will be caching styles and recalculate them
        #region Styles parsing

        public void RecalculateStyle(List<BUSSStyleProvider> providersTree, BUSSElement element) {
            element.Style.Clear();

            if (_globalStyleConfig != null) {
                ApplyConfig(element, _globalStyleConfig);
            }

            foreach (var provider in providersTree) {
                if (provider.Config != null) {
                    ApplyConfig(element, provider.Config);
                }
            }
            
            ApplyDescriptor(element, element.InlineStyle);
            
            element.ApplyStyle();
        }

        private void ApplyConfig(BUSSElement element, BUSSStyleConfig config) {
            foreach (var descriptor in config.Styles) {
                if (descriptor.Name == "*" || descriptor.Name == element.Id) {
                    ApplyDescriptor(element, descriptor);
                }
            }
        }

        private static void ApplyDescriptor(BUSSElement element, BUSSStyleDescription descriptor) {
            foreach (var property in descriptor.Properties) {
                element.Style[property.Key] = property.GetProperty();
            }
        }

        private BUSSStyle GetStyleById(string id, Dictionary<string, BUSSStyle> styleObjects)
        {
            return id != null && styleObjects.TryGetValue(id, out BUSSStyle style) ? style : new BUSSStyle();
        }

        private void ParseStyleObjects(List<BUSSStyleDescriptionWithSelector> stylesObjects, Dictionary<string, BUSSStyle> stylesDictionary)
        {
            foreach (BUSSStyleDescriptionWithSelector styleObject in stylesObjects)
            {
                if (stylesDictionary.TryGetValue(styleObject.Name, out BUSSStyle style))
                {
                    foreach (BussPropertyProvider pair in styleObject.Properties)
                    {
                        style[pair.Key] = pair.GetProperty();
                    }
                }
                else
                {
                    BUSSStyle newStyle = new BUSSStyle();
                    
                    foreach (BussPropertyProvider pair in styleObject.Properties)
                    {
                        newStyle[pair.Key] = pair.GetProperty();
                    }
                    stylesDictionary.Add(styleObject.Name, newStyle);
                }
            }
        }

        #endregion
    }
}