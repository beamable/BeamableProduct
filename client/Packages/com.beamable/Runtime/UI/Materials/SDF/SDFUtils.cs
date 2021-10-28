using Beamable.UI.SDF.Styles;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.UI;
#endif

namespace Beamable.UI.SDF {
    public static class SDFUtils {
        /// <summary>
        /// Packs SDFImage parameters in vertex data and adds quad to the vertex helper;
        /// </summary>
        public static void AddRect(this VertexHelper vh, 
            Rect position, Rect uvs, Rect backgroundUvs, Rect coords, ColorRect vertexColor,
            Vector2 size,
            float threshold,
            float rounding,
            float outlineWidth, Color outlineColor,
            Color shadowColor, float shadowThreshold, Vector2 shadowOffset) {
        
            var uv2 = new Vector2(outlineWidth,
                PackVector3ToFloat(outlineColor.r, outlineColor.g, outlineColor.b));
            var normal = new Vector3(
                threshold,
                size.x,
                size.y);
            var tangent = new Vector4(shadowOffset.x, shadowOffset.y, 
                PackVector3ToFloat((shadowThreshold / size.x) + .5f, shadowColor.a, outlineColor.a), 
                PackVector3ToFloat(shadowColor.r, shadowColor.g, shadowColor.b));
        
            AddRect(vh, position, rounding, uvs, coords, vertexColor, backgroundUvs, uv2, normal, tangent);
        }

        /// <summary>
        /// Adds quad to the vertex helper.
        /// </summary>
        private static void AddRect(
            this VertexHelper vh, 
            Rect position, 
            float z, 
            Rect uvs, 
            Rect coords, 
            ColorRect vertexColor, 
            Rect uvs1,
            Vector2 uv2,
            Vector3 normal,
            Vector4 tangent) {
            var startVertexIndex = vh.currentVertCount;
            vh.AddVert(
                new Vector3(position.xMin, position.yMin, z),
                ClipColorAlpha(vertexColor.BottomLeftColor),
                new Vector2(uvs.xMin, uvs.yMin),
                new Vector2(uvs1.xMin, uvs1.yMin),
                uv2,
                new Vector2(coords.xMin, coords.yMin),
                normal,
                tangent);
            vh.AddVert(
                new Vector3(position.xMax, position.yMin, z),
                ClipColorAlpha(vertexColor.BottomRightColor),
                new Vector2(uvs.xMax, uvs.yMin),
                new Vector2(uvs1.xMax, uvs1.yMin),
                uv2,
                new Vector2(coords.xMax, coords.yMin),
                normal,
                tangent);
            vh.AddVert(
                new Vector3(position.xMax, position.yMax, z),
                ClipColorAlpha(vertexColor.TopRightColor),
                new Vector2(uvs.xMax, uvs.yMax),
                new Vector2(uvs1.xMax, uvs1.yMax),
                uv2,
                new Vector2(coords.xMax, coords.yMax),
                normal,
                tangent);
            vh.AddVert(
                new Vector3(position.xMin, position.yMax, z),
                ClipColorAlpha(vertexColor.TopLeftColor),
                new Vector2(uvs.xMin, uvs.yMax),
                new Vector2(uvs1.xMin, uvs1.yMax),
                uv2,
                new Vector2(coords.xMin, coords.yMax),
                normal,
                tangent);
            vh.AddTriangle(startVertexIndex, startVertexIndex + 3, startVertexIndex + 2);
            vh.AddTriangle(startVertexIndex, startVertexIndex + 2, startVertexIndex + 1);
        }

        private static float PackVector3ToFloat(float x, float y, float z) {
            return PackVector3ToFloat(new Vector3(x, y, z));
        }
    
        private static float PackVector3ToFloat(this Vector3 vector) {
            return Vector3.Dot(Vector3Int.RoundToInt(vector * 255), new Vector3(65536, 256, 1));
        }

        private static Color32 ClipColorAlpha(Color32 color32) {
            if (color32.a == 0) { // hack to avoid object disappear when alpha is equal to zero
                color32.a = 1;
            }

            return color32;
        }
    }
}