using System;
using System.Collections.Generic;
using Beamable.Editor.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF {
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour {
        [SerializeField] private string _id;
        public string Id {
            get => _id;
            set {
                _id = value;
                OnValidate();
            }
        }

        #region Classes
        
        [SerializeField]
        private List<string> _classes = new List<string>();

        private IReadOnlyList<string> _readOnlyClasses;

        public IEnumerable<string> Classes => _readOnlyClasses ?? (_readOnlyClasses = _classes.AsReadOnly());

        public bool HasClass(string className) {
            return _classes.Contains(className);
        }

        public void AddClass(string className) {
            if (!_classes.Contains(className)) {
                _classes.Add(className);
                OnValidate();
            }
        }

        public void RemoveClass(string className) {
            _classes.Remove(className);
            OnValidate();
        }

        #endregion

        [SerializeField, HideInInspector]
        private BUSSElement _parent;
        [SerializeField, HideInInspector]
        private List<BUSSElement> _children = new List<BUSSElement>();

        private IEnumerable<BUSSElement> _readonlyChildren;

        public BUSSElement Parent => _parent;
        public IEnumerable<BUSSElement> Children => _readonlyChildren ?? (_readonlyChildren = _children.AsReadOnly());
        
        private void Awake() {
            FindParent();
        }

        private void OnTransformParentChanged() {
            FindParent();
        }

        private void OnDestroy() {
            ClearParent();
        }

        private void FindParent() {
            var parent = transform.parent != null ? transform.parent.GetComponentInParent<BUSSElement>() : null;
            if (parent != _parent) {
                ClearParent();

                if (parent != null && !parent._children.Contains(this)) {
                    parent._children.Add(this);
                }

                _parent = parent;
            }
        }

        private void ClearParent() {
            if (_parent != null) {
                _parent._children.Remove(this);
                _parent = null;
            }
        }

        #region TEMPORARY
        [Header("Temporary, this will be removed soon.")]
        public KeyWithProperty[] styleSheet = new KeyWithProperty[0];
        private SDFStyle _style;

        public SDFStyle GetStyle() {
            if (_style == null) {
                _style = new SDFStyle();
            }
            _style.Clear();
            foreach (var keyWithProperty in styleSheet) {
                _style[keyWithProperty.key] = keyWithProperty.property.Get<ISDFProperty>();
            }

            return _style;
        }

        void OnValidate() {
            if (TryGetComponent<SDFImage>(out var sdfImage)) {
                sdfImage.Style = GetStyle();
            }
        }
        
        [Serializable]
        public class KeyWithProperty {
            public string key;
            [SerializableValueImplements(typeof(ISDFProperty))]
            public SerializableValueObject property;
        }
        #endregion
    }
}