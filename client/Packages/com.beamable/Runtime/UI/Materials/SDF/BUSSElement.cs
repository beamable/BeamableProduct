using System;
using System.Collections.Generic;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private SDFStyleScriptableObject _styleObject;
        [SerializeField] private string _id;
#pragma warning restore CS0649

        #region Classes

        [SerializeField] private List<string> _classes = new List<string>();

        private IReadOnlyList<string> _readOnlyClasses;

        public IEnumerable<string> Classes => _readOnlyClasses ?? (_readOnlyClasses = _classes.AsReadOnly());

        public bool HasClass(string className)
        {
            return _classes.Contains(className);
        }

        public void AddClass(string className)
        {
            if (!_classes.Contains(className))
            {
                _classes.Add(className);
                Refresh();
            }
        }

        public void RemoveClass(string className)
        {
            _classes.Remove(className);
            Refresh();
        }

        #endregion

        [SerializeField, HideInInspector] private BUSSElement _parent;
        [SerializeField, HideInInspector] private List<BUSSElement> _children = new List<BUSSElement>();

        private IEnumerable<BUSSElement> _readonlyChildren;

        public BUSSElement Parent => _parent;
        public IEnumerable<BUSSElement> Children => _readonlyChildren ?? (_readonlyChildren = _children.AsReadOnly());

        private void Awake()
        {
            FindParent();
        }

        private void OnEnable()
        {
            if (_styleObject != null)
            {
                _styleObject.OnUpdate = Refresh;
                Refresh();
            }
        }

        private void OnDisable() {
            if (TryGetComponent<SDFImage>(out var sdfImage)) {
                sdfImage.Style = null;
            }
        }

        private void OnValidate()
        {
            Refresh();
        }

        private void OnTransformParentChanged()
        {
            FindParent();
        }

        private void OnDestroy()
        {
            ClearParent();
        }

        private void FindParent()
        {
            var parent = transform.parent != null ? transform.parent.GetComponentInParent<BUSSElement>() : null;
            if (parent != _parent)
            {
                ClearParent();

                if (parent != null && !parent._children.Contains(this))
                {
                    parent._children.Add(this);
                }

                _parent = parent;
            }
        }

        private void ClearParent()
        {
            if (_parent != null)
            {
                _parent._children.Remove(this);
                _parent = null;
            }
        }

        private void Refresh() {
            if (!enabled) return;
            
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                if (_styleObject != null)
                {
                    SDFStyle style = new SDFStyle();
                    
                    if (!string.IsNullOrEmpty(_id) && !string.IsNullOrWhiteSpace(_id))
                    {
                        List<KeyWithProperty> properties = _styleObject.GetProperties(_id);

                        foreach (KeyWithProperty property in properties)
                        {
                            style[property.key] = property.property.Get<ISDFProperty>();
                        }
                    }

                    foreach (string classSelector in _classes)
                    {
                        List<KeyWithProperty> properties = _styleObject.GetProperties(classSelector);

                        foreach (KeyWithProperty property in properties)
                        {
                            style[property.key] = property.property.Get<ISDFProperty>();
                        }
                    }

                    sdfImage.Style = style;
                }
            }
        }
    }
}