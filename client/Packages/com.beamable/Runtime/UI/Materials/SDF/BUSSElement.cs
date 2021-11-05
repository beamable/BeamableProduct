using System;
using System.Collections.Generic;
using Beamable.UI.Buss;
using UnityEngine;

namespace Beamable.UI.BUSS {
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour, ISerializationCallbackReceiver {
        #pragma warning disable CS0649
        [SerializeField] private string _id;
        [SerializeField] private List<string> _classes;
        [SerializeField] private BUSSStyleDescription _inlineStyle;
        [SerializeField] private BUSSStyleSheet _styleSheet;

        [SerializeField, HideInInspector] private BUSSElement _parent;
        [SerializeField, HideInInspector] private List<BUSSElement> _children;
        #pragma warning restore CS0649

        public List<BUSSStyleSheet> AllStyleSheets { get; } = new List<BUSSStyleSheet>();
        public BUSSStyle Style { get; } = new BUSSStyle();

        public string Id => _id;
        public List<string> Classes => _classes;
        public string TypeName => GetType().Name;
        public HashSet<string> PseudoTags { get; } = new HashSet<string>();

        public BUSSStyleDescription InlineStyle => _inlineStyle;
        public BUSSStyleSheet StyleSheet => _styleSheet;

        public BUSSElement Parent => _parent;

        private IReadOnlyList<BUSSElement> _childrenReadOnly;
        public IReadOnlyList<BUSSElement> Children => _childrenReadOnly ?? (_childrenReadOnly = _children.AsReadOnly());

        public void RecalculateStyleSheets() {
            AllStyleSheets.Clear();
            AddParentStyleSheets(this);
        }

        private void AddParentStyleSheets(BUSSElement element) {
            if (element.Parent != null) {
                AddParentStyleSheets(element.Parent);
            }

            if (element.StyleSheet != null) {
                AllStyleSheets.Add(element.StyleSheet);
            }
        }

        public virtual void ApplyStyle() {
            // TODO: common style implementation for BUSS Elements, so: applying all properties that affect RectTransform
        }

        private void OnEnable() {
            CheckParent();
            OnStyleChanged();
        }

        private void Reset() {
            CheckParent();
            OnStyleChanged();
        }

        private void OnValidate() {
            CheckParent();
            OnStyleChanged();
        }

        private void OnTransformParentChanged() {
            CheckParent();
            OnStyleChanged();
        }

        private void OnDisable() {
            if (Parent != null) {
                Parent._children.Remove(this);
            }
            else {
                BussConfiguration.Instance.UnregisterObserver(this);
            }
        }

        public void OnStyleChanged() {
            RecalculateStyleSheets();
            BussConfiguration.Instance.RecalculateStyle(this);
            ApplyStyle();

            foreach (BUSSElement child in Children) {
                child.OnStyleChanged();
            }
        }

        private void CheckParent() {
            var foundParent = transform.parent == null ? null : transform.parent.GetComponentInParent<BUSSElement>();
            if (Parent != null) {
                Parent._children.Remove(this);
            }
            else {
                BussConfiguration.Instance.UnregisterObserver(this);
            }

            if(!isActiveAndEnabled) return;
            
            _parent = foundParent;
            if (Parent == null) {
                BussConfiguration.Instance.RegisterObserver(this);
            }
            else {
                if (!Parent._children.Contains(this)) {
                    Parent._children.Add(this);
                } 
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                CheckParent();
                OnStyleChanged();
            };
#endif
        }
    }
}