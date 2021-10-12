using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Beamable.UI.SDF {
    [ExecuteAlways]
    public class SDFImage : Image {
        public float threshold;
        public float rounding;
        public Texture2D backgroundTexture;
        public float meshFrame;
        public float outlineWidth;
        public Color outlineColor;
        public Color shadowColor;
        public Vector2 shadowOffset;
        public float shadowThreshold;

        private Material _materialWithBackgroundTexture;
#pragma warning disable 649
        private Vector2 _uvToCoordsFactor;
#pragma warning restore 649
        private static readonly int BackgroundTexture = Shader.PropertyToID("_BackgroundTexture");

        public override Material material {
            get {
                if (backgroundTexture == null) return base.material;
            
                if (_materialWithBackgroundTexture == null) {
                    _materialWithBackgroundTexture = new Material(base.material);
                }
                _materialWithBackgroundTexture.SetTexture(BackgroundTexture, backgroundTexture);

                return _materialWithBackgroundTexture;
            }
            set => base.material = value;
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            if (type == Type.Sliced && hasBorder) {
                GenerateSlicedMesh(vh);
            }
            else {
                GenerateSpriteMesh(vh);
            }
        }

        private void GenerateSpriteMesh(VertexHelper vh) {
            vh.Clear();
            var rt = rectTransform;
            var spriteRect = sprite == null ? new Rect(Vector2.zero, Vector2.one) : GetNormalizedSpriteRect();
            var position = new Rect(
                -rt.rect.size * rt.pivot,
                rt.rect.size);
            AddRect(vh, position, spriteRect, new Rect(Vector2.zero, Vector2.one), rt.rect.size);
            AddFrame(vh, position, spriteRect);
        }

        private void GenerateSlicedMesh(VertexHelper vh) {
            vh.Clear();
            var rt = rectTransform;
            var size = rt.rect.size;
            var startPosition = -size * rt.pivot;
            var endPosition = startPosition + rt.rect.size;
            // Sprite.border returns pixel sizes of borders (in order: left, bottom, right, top).
            var borders = sprite.border / pixelsPerUnit;
        
            // Position, uv and coords values are arrays of 9-slice values in ascending order.

            var positionValues = new Vector2[] {
                startPosition,
                startPosition + new Vector2(borders.x, borders.y),
                endPosition - new Vector2(borders.z, borders.w),
                endPosition, 
            };

            var outer = DataUtility.GetOuterUV(sprite);
            var inner = DataUtility.GetInnerUV(sprite);
        
            var uvValues = new Vector2[] {
                new Vector2(outer.x, outer.y),
                new Vector2(inner.x, inner.y),
                new Vector2(inner.z, inner.w),
                new Vector2(outer.z, outer.w), 
            };
        
            var coordsValues = new Vector2[] {
                Vector2.zero, 
                new Vector2(borders.x / size.x, borders.y / size.y),
                new Vector2(borders.z / size.x, borders.w / size.y) - Vector2.one,
                Vector2.one, 
            };

            for (int xi = 0; xi < 3; xi++) {
                for (int yi = 0; yi < 3; yi++) {
                    var posMin = new Vector2(positionValues[xi].x, positionValues[yi].y);
                    var posSize = new Vector2(positionValues[xi + 1].x, positionValues[yi + 1].y) - posMin;
                    var positionRect = new Rect(posMin, posSize);
                    var uvMin = new Vector2(uvValues[xi].x, uvValues[yi].y);
                    var uvSize = new Vector2(uvValues[xi + 1].x, uvValues[yi + 1].y) - uvMin;
                    var uvRect = new Rect(uvMin, uvSize);
                    var coordsRect = new Rect(coordsValues[xi].x, coordsValues[yi].y, coordsValues[xi + 1].x, coordsValues[yi + 1].y);
                    AddRect(vh, positionRect, uvRect, coordsRect, size);
                }
            }
        
            AddFrame(vh, 
                new Rect(startPosition, endPosition - startPosition), 
                new Rect(uvValues[0], uvValues[3]));
        }

        private Rect GetNormalizedSpriteRect() {
            var spriteRect = sprite.rect;
            spriteRect.x /= sprite.texture.width;
            spriteRect.width /= sprite.texture.width;
            spriteRect.y /= sprite.texture.height;
            spriteRect.height /= sprite.texture.height;
            return spriteRect;
        }

        /// <summary>
        /// Adds vertices and triangles to VertexHelper around given rect.
        /// </summary>
        private void AddFrame(VertexHelper vh, Rect position, Rect uv) {
            if (meshFrame < .01f) return;
            var size = rectTransform.rect.size;
            var doubledFrame = meshFrame * 2f;
            // GrownPosition and GrownUV are outer rects of the frame.
            var grownPosition = new Rect(
                position.x - meshFrame, position.y - meshFrame, 
                position.size.x + doubledFrame, position.size.y + doubledFrame);
            var grownUV = new Rect(
                uv.xMin - meshFrame / size.x,
                uv.yMin - meshFrame / size.y,
                uv.width * grownPosition.width / position.width,
                uv.height * grownPosition.height / position.height);
            var ratio = new Vector2(meshFrame, meshFrame) / size;
            var grownCoords = new Rect(- ratio, 2f * ratio + Vector2.one);
        
            // A, B, C and D are left, right, bottom and top parts of the frame.
            // Left and right are contains top and bottom corners, while bottom and top are shorter.
        
            var posA = Rect.MinMaxRect(grownPosition.xMin, grownPosition.yMin, position.xMin, grownPosition.yMax);
            var posB = Rect.MinMaxRect(position.xMax, grownPosition.yMin, grownPosition.xMax, grownPosition.yMax);
            var posC = Rect.MinMaxRect(position.xMin, grownPosition.yMin, position.xMax, position.yMin);
            var posD = Rect.MinMaxRect(position.xMin, position.yMax, position.xMax, grownPosition.yMax);
        
            var uvA = Rect.MinMaxRect(grownUV.xMin, grownUV.yMin, uv.xMin, grownUV.yMax);
            var uvB = Rect.MinMaxRect(uv.xMax, grownUV.yMin, grownUV.xMax, grownUV.yMax);
            var uvC = Rect.MinMaxRect(uv.xMin, grownUV.yMin, uv.xMax, uv.yMin);
            var uvD = Rect.MinMaxRect(uv.xMin, uv.yMax, uv.xMax, grownUV.yMax);
        
            var coordsA = Rect.MinMaxRect(grownCoords.xMin, grownCoords.yMin, 0f, grownCoords.yMax);
            var coordsB = Rect.MinMaxRect(1f, grownCoords.yMin, grownCoords.xMax, grownCoords.yMax);
            var coordsC = Rect.MinMaxRect(0f, grownCoords.yMin, 1f, 0f);
            var coordsD = Rect.MinMaxRect(0f, 1f, 1f, grownCoords.yMax);
        
            AddRect(vh, posA, uvA, coordsA, size);
            AddRect(vh, posB, uvB, coordsB, size);
            AddRect(vh, posC, uvC, coordsC, size);
            AddRect(vh, posD, uvD, coordsD, size);
        }

        private void AddRect(VertexHelper vh, Rect position, Rect spriteRect, Rect coordsRect, Vector2 size) {
            vh.AddRect(
                position,
                spriteRect,
                coordsRect,
                color,
                size,
                threshold,
                _uvToCoordsFactor,
                rounding,
                outlineWidth, outlineColor,
                shadowColor, shadowThreshold, shadowOffset
            );
        }
    }
}