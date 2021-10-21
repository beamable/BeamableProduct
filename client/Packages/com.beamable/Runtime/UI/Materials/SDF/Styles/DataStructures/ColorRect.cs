using System;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    [Serializable]
    public struct ColorRect {
        public Color BottomLeftColor;
        public Color BottomRightColor;
        public Color TopLeftColor;
        public Color TopRightColor;
        
        public ColorRect(Color bottomLeftColor, Color bottomRightColor, Color topLeftColor, Color topRightColor) {
            BottomLeftColor = bottomLeftColor;
            BottomRightColor = bottomRightColor;
            TopLeftColor = topLeftColor;
            TopRightColor = topRightColor;
        }

        public ColorRect(Color color = default) : this(color, color, color, color) { }
    }
}