using System;
using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.Tweening;
using UnityEngine;

namespace Beamable.UI.Buss {
    [ExecuteAlways, DisallowMultipleComponent]
    public class BussElement : MonoBehaviour {
#pragma warning disable CS0649
        [SerializeField] private string _id;
        [SerializeField] private List<string> _classes;
        [SerializeField] private BussStyleDescription _inlineStyle;
        [SerializeField] private BussStyleSheet _styleSheet;

        [SerializeField] private PseudoClassDefinition[] _pseudoClassDefinitions = new PseudoClassDefinition[0];

        [SerializeField, HideInInspector] private BussElement _parent;
        [SerializeField, HideInInspector] private List<BussElement> _children = new List<BussElement>();
#pragma warning restore CS0649

        public List<BussStyleSheet> AllStyleSheets { get; } = new List<BussStyleSheet>();
        public BussStyle Style { get; } = new BussStyle();

        public string Id => _id;
        public List<string> Classes => _classes;
        public string TypeName => GetType().Name;
        public Dictionary<string, BussStyle> PseudoStyles { get; } = new Dictionary<string, BussStyle>();

        public BussStyleDescription InlineStyle => _inlineStyle;
        public BussStyleSheet StyleSheet => _styleSheet;

        public BussElement Parent => _parent;

        private IReadOnlyList<BussElement> _childrenReadOnly;
        public IReadOnlyList<BussElement> Children
        {
	        get
	        {
		        if (_childrenReadOnly == null)
		        {
			        if (_children == null)
			        {
				        _children = new List<BussElement>();
			        }

			        _childrenReadOnly = _children.AsReadOnly();
		        }

		        return _childrenReadOnly;
	        }
        }
        
        public void AddClass(string id)
        {
	        if (!_classes.Contains(id))
	        {
		        _classes.Add(id);
		        OnStyleChanged();
	        }
	        
        }

        public void RemoveClass(string id)
        {
	        if (_classes.Contains(id))
	        {
		        _classes.Remove(id);
		        OnStyleChanged();
	        }
        }

        public void RecalculateStyleSheets() {
            AllStyleSheets.Clear();
            AddParentStyleSheets(this);
        }

        private void AddParentStyleSheets(BussElement element) {
            if (element.Parent != null) {
                AddParentStyleSheets(element.Parent);
            }

            if (element.StyleSheet != null) {
                AllStyleSheets.Add(element.StyleSheet);
            }
        }

        public BussStyle GetCombinedStyle() {
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

            foreach (BussElement child in Children) {
                child.OnStyleChanged();
            }
        }

        public void CheckParent() {
            var foundParent = (transform == null || transform.parent == null)
                ? null
                : transform.parent.GetComponentInParent<BussElement>();
            if (Parent != null) {
                Parent._children.Remove(this);
            }
            else {
                BussConfiguration.Instance.UnregisterObserver(this);
            }

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
            public BussStyleDescription styleSheet;
            private BussPseudoStyle _style;

            public BussStyle ApplyStyle(BussStyle baseStyle) {
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

            public void Toggle(BussElement element) {
                var style = element.GetCombinedStyle();
                var duration = BussStyle.TransitionDuration.Get(style).FloatValue;
                var easing = BussStyle.TransitionEasing.Get(style).Enum;
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
