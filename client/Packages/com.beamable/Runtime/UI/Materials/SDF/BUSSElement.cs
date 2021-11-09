using System;
using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.Tweening;
using UnityEngine;

namespace Beamable.UI.BUSS {
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour {
#pragma warning disable CS0649
        [SerializeField] private string _id;
        [SerializeField] private List<string> _classes;
        [SerializeField] private BUSSStyleDescription _inlineStyle;
        [SerializeField] private BUSSStyleSheet _styleSheet;

        [SerializeField] private PseudoClassDefinition[] _pseudoClassDefinitions = new PseudoClassDefinition[0];

        [SerializeField, HideInInspector] private BUSSElement _parent;
        [SerializeField, HideInInspector] private List<BUSSElement> _children = new List<BUSSElement>();
#pragma warning restore CS0649

        public List<BUSSStyleSheet> AllStyleSheets { get; } = new List<BUSSStyleSheet>();
        public BUSSStyle Style { get; } = new BUSSStyle();

        public string Id => _id;
        public List<string> Classes => _classes;
        public string TypeName => GetType().Name;
        public Dictionary<string, BUSSStyle> PseudoStyles { get; } = new Dictionary<string, BUSSStyle>();

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

        public BUSSStyle GetCombinedStyle() {
            var style = Style;
            foreach (var pseudoClassDefinition in _pseudoClassDefinitions) {
                style = pseudoClassDefinition.ApplyStyle(style);
            }

            return style;
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
            var foundParent = (transform == null || transform.parent == null)
                ? null
                : transform.parent.GetComponentInParent<BUSSElement>();
            if (Parent != null) {
                Parent._children.Remove(this);
            }
            else {
                BussConfiguration.Instance.UnregisterObserver(this);
            }

            if (!isActiveAndEnabled) return;

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

        [Serializable]
        public class PseudoClassDefinition {
            public bool animate;
            public bool enabled;
            [Range(0f, 1f)] public float blending;
            public BUSSStyleDescription styleSheet;
            private BussPseudoStyle _style;

            public BUSSStyle ApplyStyle(BUSSStyle baseStyle) {
                if (enabled) {
                    if (_style == null) {
                        _style = new BussPseudoStyle(baseStyle);
                    }

                    _style.BaseStyle = baseStyle;
                    _style.BlendValue = blending;

                    _style.Clear();

                    foreach (var property in styleSheet.Properties) {
                        _style[property.Key] = property.GetProperty();
                    }

                    return _style;
                }

                return baseStyle;
            }

            public void Toggle(BUSSElement element) {
                var style = element.GetCombinedStyle();
                var duration = BUSSStyle.TransitionDuration.Get(style).FloatValue;
                var easing = BUSSStyle.TransitionEasing.Get(style).Enum;
                var wasEnables = enabled;
                enabled = true;
                var tween = new FloatTween(duration, wasEnables ? 1f : 0f, wasEnables ? 0f : 1f, f => {
                    blending = f;
                    element.OnStyleChanged();
                });
                tween.SetEasing(easing);
                tween.CompleteEvent += () => {
                    enabled = !wasEnables;
                    element.OnStyleChanged();
                };
                tween.Run();
            }
        }

        private void Update() {
            if (Application.isPlaying) {
                foreach (var definition in _pseudoClassDefinitions) {
                    if (definition.animate) {
                        definition.animate = false;
                        definition.Toggle(this);
                    }
                }
            }
        }
    }
}