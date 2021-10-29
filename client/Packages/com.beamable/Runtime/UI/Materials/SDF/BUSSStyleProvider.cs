using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    [RequireComponent(typeof(BUSSElement))]
    public class BUSSStyleProvider : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private BUSSStyleConfig _config;
#pragma warning restore CS0649

        // TODO: serialized for debug purposes only. Remove before final push
        [SerializeField] private List<BUSSStyleProvider> _childProviders = new List<BUSSStyleProvider>();
        [SerializeField] private BUSSStyleProvider _parentProvider;
        [SerializeField] private BUSSElement _bussElement;

        public void OnGlobalStyleChanged(List<BUSSStyleDescription> globalStyles)
        {
            Debug.Log($"{name}: Global style changed");
            
            // TODO: take local style and apply to local buss element

            foreach (BUSSStyleProvider childProvider in _childProviders)
            {
                childProvider.OnParentStyleChanged(globalStyles);
            }
        }

        private void OnParentStyleChanged(List<BUSSStyleDescription> parentStyles)
        {
            Debug.Log($"{name}: Parent style changed");
            
            Dictionary<string,BUSSStyle> styles = ParseStyles(parentStyles);
            
            if (_bussElement != null)
            {
                _bussElement.ApplyStyle(GetStyleById(_bussElement.Id, styles));
            }

            foreach (BUSSStyleProvider childProvider in _childProviders)
            {
                childProvider.OnParentStyleChanged(parentStyles);
            }
        }

        private void OnLocalStyleChanged(List<BUSSStyleDescription> localStyles)
        {
            Debug.Log($"{name}: Local style changed");

            Dictionary<string,BUSSStyle> styles = ParseStyles(localStyles);
            
            if (_bussElement != null)
            {
                _bussElement.ApplyStyle(GetStyleById(_bussElement.Id, styles));
            }

            foreach (BUSSStyleProvider childProvider in _childProviders)
            {
                childProvider.OnParentStyleChanged(localStyles);
            }
        }
        
        private BUSSStyle GetStyleById(string id, Dictionary<string, BUSSStyle> styleObjects)
        {
            return styleObjects.TryGetValue(id, out BUSSStyle style) ? style : new BUSSStyle();
        }
        
        private Dictionary<string, BUSSStyle> ParseStyles(List<BUSSStyleDescription> stylesList)
        {
            Dictionary<string, BUSSStyle> styles = new Dictionary<string, BUSSStyle>();
            ParseStyleObjects(stylesList, ref styles);
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
        
        private void OnBeforeTransformParentChanged()
        {
            UnregisterFromParent();
        }

        private void OnTransformParentChanged()
        {
            RegisterToParent();
        }

        private void OnValidate()
        {
            if (_bussElement == null)
            {
                _bussElement = GetComponent<BUSSElement>();
            }
            
            if (_config != null)
            {
                _config.OnChange += OnLocalStyleChanged;
            }
            
            RegisterToParent();
        }

        private void OnEnable()
        {
            if (_config != null)
            {
                _config.OnChange += OnLocalStyleChanged;
            }

            RegisterToParent();
        }

        private void OnDisable()
        {
            if (_config != null)
            {
                _config.OnChange -= OnLocalStyleChanged;
            }

            UnregisterFromParent();
        }

        private void OnDestroy()
        {
            if (_config != null)
            {
                _config.OnChange -= OnLocalStyleChanged;
            }
            
            UnregisterFromParent();
        }

        private void LookForParentStyleProvider()
        {
            Transform currentTransform = gameObject.transform;

            while (_parentProvider == null)
            {
                if (currentTransform.parent == null)
                {
                    break;
                }

                currentTransform = currentTransform.parent;

                BUSSStyleProvider styleProvider = currentTransform.GetComponent<BUSSStyleProvider>();
                _parentProvider = styleProvider;
            }
        }

        private void RegisterToParent()
        {
            if (_parentProvider == null)
            {
                LookForParentStyleProvider();
            }

            if (_parentProvider != null)
            {
                _parentProvider.RegisterObserver(this);
            }
            else
            {
                BussConfiguration.Instance.RegisterObserver(this);
            }
        }

        private void UnregisterFromParent()
        {
            if (_parentProvider != null)
            {
                _parentProvider.UnregisterObserver(this);
                _parentProvider = null;
            }
            else
            {
                BussConfiguration.Instance.UnregisterObserver(this);
            }
        }

        private void RegisterObserver(BUSSStyleProvider childProvider)
        {
            if (!_childProviders.Contains(childProvider))
            {
                _childProviders.Add(childProvider);
            }
        }

        private void UnregisterObserver(BUSSStyleProvider childProvider)
        {
            if (_childProviders.Contains(childProvider))
            {
                _childProviders.Remove(childProvider);
            }
        }
    }
}