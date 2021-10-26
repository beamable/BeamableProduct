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
        
        public ColorRect TopEdgeRect => new ColorRect(TopLeftColor, TopRightColor, TopLeftColor, TopRightColor);
        public ColorRect BottomEdgeRect => new ColorRect(BottomLeftColor, BottomRightColor, BottomLeftColor, BottomRightColor);
        public ColorRect LeftEdgeRect => new ColorRect(BottomLeftColor, BottomLeftColor, TopLeftColor, TopLeftColor);
        public ColorRect RightEdgeRect => new ColorRect(BottomRightColor, BottomRightColor, TopRightColor, TopRightColor);
    }
}