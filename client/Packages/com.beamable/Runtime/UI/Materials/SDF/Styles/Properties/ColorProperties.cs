using System;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    
    // BUSS: color: #232323
    [Serializable]
    public class SingleColorProperty : IColorProperty, IVertexColorProperty {
        [SerializeField]
        private Color _color;

        public Color Color {
            get => _color;
            set => _color = value;
        }

        public SingleColorProperty() { }

        public SingleColorProperty(Color color) {
            Color = color;
        }

        public ColorRect ColorRect => new ColorRect(_color);
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
    }
}