using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class SDFStyleProvider : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private SDFStyleConfig _config;
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
            List<SingleStyleObject> globalStyleObjects = BussConfiguration.Instance.GetGlobalStyles();
            List<SingleStyleObject> localStyleObjects = GetLocalStyles();

            Dictionary<string, SDFStyle> parsedStyles = ParseStyles(globalStyleObjects, localStyleObjects);

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

        private List<SingleStyleObject> GetLocalStyles()
        {
            return _config ? _config.Styles : new List<SingleStyleObject>();
        }

        private SDFStyle GetStyleById(string id, Dictionary<string, SDFStyle> styleObjects)
        {
            return styleObjects.TryGetValue(id, out SDFStyle style) ? style : new SDFStyle();
        }

        private Dictionary<string, SDFStyle> ParseStyles(List<SingleStyleObject> globalStyles,
            List<SingleStyleObject> localStyles)
        {
            Dictionary<string, SDFStyle> styles = new Dictionary<string, SDFStyle>();
            ParseStyleObjects(globalStyles, ref styles);
            ParseStyleObjects(localStyles, ref styles);
            return styles;
        }

        private void ParseStyleObjects(List<SingleStyleObject> stylesObjects, ref Dictionary<string, SDFStyle> stylesDictionary)
        {
            foreach (SingleStyleObject styleObject in stylesObjects)
            {
                if (stylesDictionary.TryGetValue(styleObject.Name, out SDFStyle style))
                {
                    foreach (KeyWithProperty pair in styleObject.Properties)
                    {
                        style[pair.key] = pair.property.Get<ISDFProperty>();
                    }
                }
                else
                {
                    SDFStyle newStyle = new SDFStyle();
                    
                    foreach (KeyWithProperty pair in styleObject.Properties)
                    {
                        newStyle[pair.key] = pair.property.Get<ISDFProperty>();
                    }
                    stylesDictionary.Add(styleObject.Name, newStyle);
                }
            }
        }
    }
}