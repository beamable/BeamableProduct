using System;
using System.Collections.Generic;
using Beamable.UI.Buss;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.UI.BUSS
{
    [ExecuteInEditMode, DisallowMultipleComponent, Obsolete]
    public class BUSSStyleProvider : MonoBehaviour
    {
#pragma warning disable CS0649
[FormerlySerializedAs("_config")]
        [SerializeField] private BUSSStyleSheet sheet;
#pragma warning restore CS0649

        private readonly List<BUSSStyleProvider> _childProviders = new List<BUSSStyleProvider>();
        public List<BUSSStyleProvider> _providersTree = new List<BUSSStyleProvider>();
        private BUSSStyleProvider _parentProvider;
        private BUSSElement _bussElement;

        public BUSSStyleSheet Sheet => sheet;
        private BUSSStyleProvider ParentProvider => _parentProvider;

        public void OnStyleChanged()
        {
            // if (_bussElement != null)
            // {
            //     BussConfiguration.Instance.RecalculateStyle(_providersTree, _bussElement);
            //     _bussElement.ApplyStyle();
            // }
            //
            // foreach (BUSSStyleProvider childProvider in _childProviders)
            // {
            //     childProvider.OnStyleChanged();
            // }
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

            if (Sheet != null)
            {
                Sheet.OnChange += OnStyleChanged;
            }

            RegisterToParent();
        }

        private void OnEnable()
        {
            if (Sheet != null)
            {
                Sheet.OnChange += OnStyleChanged;
            }

            RegisterToParent();
        }

        private void OnDisable()
        {
            if (Sheet != null)
            {
                Sheet.OnChange -= OnStyleChanged;
            }

            UnregisterFromParent();
        }

        private void OnDestroy()
        {
            if (Sheet != null)
            {
                Sheet.OnChange -= OnStyleChanged;
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
                // BussConfiguration.Instance.RegisterObserver(this);
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
                // BussConfiguration.Instance.UnregisterObserver(this);
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
            _providersTree.Reverse();
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