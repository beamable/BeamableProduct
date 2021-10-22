using System;
using Beamable.Editor.UI.SDF;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.UI.SDF.Styles {
    [ExecuteAlways]
    public class SDFTest : MonoBehaviour {
        public KeyWithProperty[] styleSheet;
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

        void Update() {
            var style = GetStyle();
            if (TryGetComponent<SDFImage>(out var sdfImage)) {
                var size = sdfImage.rectTransform.rect.size;
                var minSize = Mathf.Min(size.x, size.y);
                sdfImage.colorRect = SDFStyle.BackgroundColor.Get(style).ColorRect;
                sdfImage.rounding = SDFStyle.RoundCorners.Get(style).GetFloatValue(minSize);

                sdfImage.outlineWidth = SDFStyle.BorderWidth.Get(style).FloatValue;
                sdfImage.outlineColor = SDFStyle.BorderColor.Get(style).Color;
            
                sdfImage.SetVerticesDirty();
            }
        }
        
        [Serializable]
        public class KeyWithProperty {
            public string key;
            [SerializableValueImplements(typeof(ISDFProperty))]
            public SerializableValueObject property;
        }
    }
}