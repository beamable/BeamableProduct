using System;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    [Serializable]
    public struct ColorRect {
        public Color BottomLeftColor;
        public Color BottomRightColor;
        public Color TopLeftColor;
        public Color TopRightColor;

#if UNITY_EDITOR
        #pragma warning disable CS0414
        [SerializeField] private int _drawerMode;
        #pragma warning restore CS0414
#endif
        
        public ColorRect(Color bottomLeftColor, Color bottomRightColor, Color topLeftColor, Color topRightColor) {
            BottomLeftColor = bottomLeftColor;
            BottomRightColor = bottomRightColor;
            TopLeftColor = topLeftColor;
            TopRightColor = topRightColor;
            _drawerMode = 0;
        }

        public ColorRect(Color color = default) : this(color, color, color, color) { }
        
        public ColorRect TopEdgeRect => new ColorRect(TopLeftColor, TopRightColor, TopLeftColor, TopRightColor);
        public ColorRect BottomEdgeRect => new ColorRect(BottomLeftColor, BottomRightColor, BottomLeftColor, BottomRightColor);
        public ColorRect LeftEdgeRect => new ColorRect(BottomLeftColor, BottomLeftColor, TopLeftColor, TopLeftColor);
        public ColorRect RightEdgeRect => new ColorRect(BottomRightColor, BottomRightColor, TopRightColor, TopRightColor);
    }
}