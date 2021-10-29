using System;
using Beamable.Editor.UI.SDF;
using Beamable.UI.SDF;
using Beamable.UI.SDF.Styles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.UI.BUSS {
    [ExecuteAlways]
    public class BUSSTest : MonoBehaviour {
        public KeyWithProperty[] styleSheet;
        private BUSSStyle _style;

        public BUSSStyle GetStyle() {
            if (_style == null) {
                _style = new BUSSStyle();
            }
            _style.Clear();
            foreach (var keyWithProperty in styleSheet) {
                _style[keyWithProperty.key] = keyWithProperty.property.Get<IBUSSProperty>();
            }

            return _style;
        }

        void Update() {
            var style = GetStyle();
            if (TryGetComponent<SDFImage>(out var sdfImage)) {
                var size = sdfImage.rectTransform.rect.size;
                var minSize = Mathf.Min(size.x, size.y);
                sdfImage.colorRect = BUSSStyle.BackgroundColor.Get(style).ColorRect;
                sdfImage.rounding = BUSSStyle.RoundCorners.Get(style).GetFloatValue(minSize);

                sdfImage.outlineWidth = BUSSStyle.BorderWidth.Get(style).FloatValue;
                sdfImage.outlineColor = BUSSStyle.BorderColor.Get(style).Color;
            
                sdfImage.SetVerticesDirty();
            }
        }
        
        [Serializable]
        public class KeyWithProperty {
            public string key;
            [SerializableValueImplements(typeof(IBUSSProperty))]
            public SerializableValueObject property;
        }
    }
}