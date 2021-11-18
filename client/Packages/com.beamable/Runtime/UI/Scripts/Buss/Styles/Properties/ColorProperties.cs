using System;
using Beamable.UI.Sdf.Styles;
using UnityEngine;

namespace Beamable.UI.Buss {
    
    // BUSS: color: #232323
    [Serializable]
    public class SingleColorProperty : IColorProperty, IVertexColorProperty {
        [SerializeField]
        private Color _color;

        public Color Color {
            get => _color;
            set => _color = value;
        }

        public ColorRect ColorRect => new ColorRect(_color);

        public SingleColorProperty() { }

        public SingleColorProperty(Color color) {
            Color = color;
        }
        
        public IBussProperty Clone() {
            return new SingleColorProperty(_color);
        }

        public IBussProperty Interpolate(IBussProperty other, float value) {
            if (other is IColorProperty col) {
                return new SingleColorProperty(Color.Lerp(_color, col.Color, value));
            }
            if (other is IVertexColorProperty vert) {
                return new VertexColorProperty(ColorRect.Lerp(new ColorRect(_color), vert.ColorRect, value));
            }

            return Clone();
        }
    }

    // BUSS: color: #232323 #a3a3a3 #a3a3a3 #241321
    [Serializable]
    public class VertexColorProperty : IVertexColorProperty {
        [SerializeField]
        public ColorRect _colorRect;

        public ColorRect ColorRect {
            get => _colorRect;
            set => _colorRect = value;
        }
        
        public VertexColorProperty() { }
        
        public VertexColorProperty(Color color) {
            _colorRect = new ColorRect(color);
        }
        
        public VertexColorProperty(Color bl, Color br, Color tl, Color tr) {
            _colorRect = new ColorRect(bl, br, tl ,tr);
        }

        public VertexColorProperty(ColorRect colorRect) {
            _colorRect = colorRect;
        }

        public IBussProperty Clone() {
            return new VertexColorProperty(_colorRect);
        }

        public IBussProperty Interpolate(IBussProperty other, float value) {
            if (other is IVertexColorProperty vert) {
                return new VertexColorProperty(ColorRect.Lerp(ColorRect, vert.ColorRect, value));
            }
            if (other is IColorProperty col) {
                return new VertexColorProperty(ColorRect.Lerp(ColorRect, new ColorRect(col.Color), value));
            }

            return Clone();
        }
    }
}