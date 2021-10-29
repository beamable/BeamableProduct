using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class BUSSStyleProvider : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private BUSSStyleConfig _config;
#pragma warning restore CS0649

        // TODO: remove SerializeField attribute
        [SerializeField] private List<BUSSElement> _bussElements = new List<BUSSElement>();

        public void Register(BUSSElement bussElement)
        {
            if (!_bussElements.Contains(bussElement))
            {
                _bussElements.Add(bussElement);
            }
            
            Setup();
        }

        public void Unregister(BUSSElement bussElement)
        {
            if (_bussElements.Contains(bussElement))
            {
                _bussElements.Remove(bussElement);
            }
            
            Setup();
        }

        public void NotifyOnStyleChanged()
        {
            // TODO: Prepare cascade styles from global and context configs
            List<BUSSStyleDescription> globalStyleObjects = BussConfiguration.Instance.GetGlobalStyles();
            List<BUSSStyleDescription> localStyleObjects = GetLocalStyles();

            Dictionary<string, BUSSStyle> parsedStyles = ParseStyles(globalStyleObjects, localStyleObjects);

            foreach (BUSSElement bussElement in _bussElements)
            {
                bussElement.NotifyOnStyleChanged(GetStyleById(bussElement.Id, parsedStyles));
            }
        }

        private void OnValidate()
        {
            Setup();
        }

        private void OnEnable()
        {
            Setup();
        }

        private void OnDisable()
        {
            BussConfiguration.Instance.UnregisterObserver(this);
        }

        private void Setup()
        {
            if (_bussElements.Count > 0)
            {
                BussConfiguration.Instance.RegisterObserver(this);
                if (_config != null)
                {
                    _config.OnChange = NotifyOnStyleChanged;
                    NotifyOnStyleChanged();
                }
            }
            else
            {
                BussConfiguration.Instance.UnregisterObserver(this);
                if (_config != null)
                {
                    _config.OnChange = null;
                }
            }
        }

        private List<BUSSStyleDescription> GetLocalStyles()
        {
            return _config ? _config.Styles : new List<BUSSStyleDescription>();
        }

        private BUSSStyle GetStyleById(string id, Dictionary<string, BUSSStyle> styleObjects)
        {
            return id != null && styleObjects.TryGetValue(id, out BUSSStyle style) ? style : new BUSSStyle();
        }

        private Dictionary<string, BUSSStyle> ParseStyles(List<BUSSStyleDescription> globalStyles,
            List<BUSSStyleDescription> localStyles)
        {
            Dictionary<string, BUSSStyle> styles = new Dictionary<string, BUSSStyle>();
            ParseStyleObjects(globalStyles, ref styles);
            ParseStyleObjects(localStyles, ref styles);
            return styles;
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
    }
}