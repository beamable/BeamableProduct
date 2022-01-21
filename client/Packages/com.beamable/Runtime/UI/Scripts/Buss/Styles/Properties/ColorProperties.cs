using System;
using Beamable.UI.Sdf.Styles;
using UnityEngine;

namespace Beamable.UI.Buss {
    
    // BUSS: color: #232323
    [Serializable]
    public class SingleColorBussProperty : IColorBussProperty, IVertexColorBussProperty {
        [SerializeField]
        private Color _color;

        public Color Color {
            get => _color;
            set => _color = value;
        }

        public ColorRect ColorRect => new ColorRect(_color);

        public SingleColorBussProperty() { }

        public SingleColorBussProperty(Color color) {
            Color = color;
        }
        
        public IBussProperty CopyProperty() {
            return new SingleColorBussProperty(_color);
        }

        public IBussProperty Interpolate(IBussProperty other, float value) {
            if (other is IColorBussProperty col) {
                return new SingleColorBussProperty(Color.Lerp(_color, col.Color, value));
            }
            if (other is IVertexColorBussProperty vert) {
                return new VertexColorBussProperty(ColorRect.Lerp(new ColorRect(_color), vert.ColorRect, value));
            }

            return CopyProperty();
        }
    }

    // BUSS: color: #232323 #a3a3a3 #a3a3a3 #241321
    [Serializable]
    public class VertexColorBussProperty : IVertexColorBussProperty {
        [SerializeField]
        private ColorRect _colorRect;

        public ColorRect ColorRect {
            get => _colorRect;
            set => _colorRect = value;
        }
        
        public VertexColorBussProperty() { }
        
        public VertexColorBussProperty(Color color) {
            _colorRect = new ColorRect(color);
        }
        
        public VertexColorBussProperty(Color bl, Color br, Color tl, Color tr) {
            _colorRect = new ColorRect(bl, br, tl ,tr);
        }

        public VertexColorBussProperty(ColorRect colorRect) {
            _colorRect = colorRect;
        }

        public IBussProperty CopyProperty() {
            return new VertexColorBussProperty(_colorRect);
        }

        public IBussProperty Interpolate(IBussProperty other, float value) {
            if (other is IVertexColorBussProperty vert) {
                return new VertexColorBussProperty(ColorRect.Lerp(ColorRect, vert.ColorRect, value));
            }
            if (other is IColorBussProperty col) {
                return new VertexColorBussProperty(ColorRect.Lerp(ColorRect, new ColorRect(col.Color), value));
            }

            return CopyProperty();
        }
    }
}
