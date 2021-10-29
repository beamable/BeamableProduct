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
#pragma warning disable CS0649
        [SerializeField] private BUSSStyleConfig _globalStyleConfig;
#pragma warning restore CS0649

        // TODO: serialized only for debug purposes. Remove before final push
        [SerializeField] private List<BUSSStyleProvider> _styleProviders = new List<BUSSStyleProvider>();

        public void RegisterObserver(BUSSStyleProvider styleProvider)
        {
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
                styleProvider.OnGlobalStyleChanged();
            }
        }

        // TODO: in future move to some styles repository class which responsibility will be caching styles and recalculate them
        #region Styles parsing

        public BUSSStyle PrepareStyle(List<BUSSStyleProvider> providersTree, string bussElementId)
        {
            Dictionary<string, BUSSStyle> styles = new Dictionary<string, BUSSStyle>();
            ParseStyleObjects(_globalStyleConfig.Styles, ref styles);
            return GetStyleById(bussElementId, styles);
        }

        private Dictionary<string, BUSSStyle> ParseStyles(List<BUSSStyleDescription> stylesList)
        {
            Dictionary<string, BUSSStyle> styles = new Dictionary<string, BUSSStyle>();
            ParseStyleObjects(stylesList, ref styles);
            return styles;
        }

        private BUSSStyle GetStyleById(string id, Dictionary<string, BUSSStyle> styleObjects)
        {
            return id != null && styleObjects.TryGetValue(id, out BUSSStyle style) ? style : new BUSSStyle();
        }

        private void ParseStyleObjects(List<BUSSStyleDescription> stylesObjects, ref Dictionary<string, BUSSStyle> stylesDictionary)
        {
            foreach (BUSSStyleDescription styleObject in stylesObjects)
            {
                if (stylesDictionary.TryGetValue(styleObject.Name, out BUSSStyle style))
                {
                    foreach (BUSSProperty pair in styleObject.Properties)
                    {
                        style[pair.key] = pair.property.Get<IBUSSProperty>();
                    }
                }
                else
                {
                    BUSSStyle newStyle = new BUSSStyle();
                    
                    foreach (BUSSProperty pair in styleObject.Properties)
                    {
                        newStyle[pair.key] = pair.property.Get<IBUSSProperty>();
                    }
                    stylesDictionary.Add(styleObject.Name, newStyle);
                }
            }
        }

        #endregion
    }
}