using System.Collections.Generic;
using Beamable.UI.Buss;
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
        [SerializeField] private List<BUSSStyleProvider> _providersTree = new List<BUSSStyleProvider>();

        private BUSSStyleProvider ParentProvider => _parentProvider;

        public void OnGlobalStyleChanged()
        {
            // Debug.Log($"{name}: Global style changed");

            // TODO: take local style and apply to local buss element

            foreach (BUSSStyleProvider childProvider in _childProviders)
            {
                childProvider.OnParentStyleChanged();
            }
        }

        private void OnParentStyleChanged()
        {
            // Debug.Log($"{name}: Parent style changed");

            if (_bussElement != null)
            {
                BUSSStyle style = BussConfiguration.Instance.PrepareStyle(_providersTree, _bussElement.Id);
                _bussElement.ApplyStyle(style);
            }

            foreach (BUSSStyleProvider childProvider in _childProviders)
            {
                childProvider.OnParentStyleChanged();
            }
        }

        private void OnLocalStyleChanged()
        {
            // Debug.Log($"{name}: Local style changed");

            if (_bussElement != null)
            {
                BUSSStyle style = BussConfiguration.Instance.PrepareStyle(_providersTree, _bussElement.Id);
                _bussElement.ApplyStyle(style);
            }

            foreach (BUSSStyleProvider childProvider in _childProviders)
            {
                childProvider.OnParentStyleChanged();
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

            while (ParentProvider == null)
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
            if (ParentProvider == null)
            {
                LookForParentStyleProvider();
            }

            if (ParentProvider != null)
            {
                ParentProvider.RegisterObserver(this);
            }
            else
            {
                BussConfiguration.Instance.RegisterObserver(this);
            }

            BuildParentProvidersTree();
        }

        private void UnregisterFromParent()
        {
            if (ParentProvider != null)
            {
                ParentProvider.UnregisterObserver(this);
                _parentProvider = null;
            }
            else
            {
                BussConfiguration.Instance.UnregisterObserver(this);
            }

            _providersTree.Clear();
        }

        private void BuildParentProvidersTree()
        {
            _providersTree.Clear();

            BUSSStyleProvider currentProvider = this;
            while (currentProvider != null)
            {
                _providersTree.Add(currentProvider);
                currentProvider = currentProvider.ParentProvider != null ? currentProvider.ParentProvider : null;
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