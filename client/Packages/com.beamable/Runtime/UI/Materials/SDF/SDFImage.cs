using Beamable.UI.BUSS;
using Beamable.UI.SDF.MaterialManagement;
using Beamable.UI.SDF.Styles;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Beamable.UI.SDF {
    [ExecuteAlways]
    public class SDFImage : Image {

        private BUSSStyle _style;
        public BUSSStyle Style {
            get => _style;
            set {
                _style = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public ImageType imageType;
        public SdfMode mode;
        public ColorRect colorRect = new ColorRect(Color.white);
        public float threshold;
        public float rounding;
        public Sprite backgroundSprite;
        public SdfBackgroundMode backgroundMode;
        public float meshFrame;
        public float outlineWidth;
        public ColorRect outlineColor;
        public ColorRect shadowColor;
        public Vector2 shadowOffset;
        public float shadowThreshold;
        public float shadowSoftness;
        public SdfShadowMode shadowMode;

        public override Material material {
            get {
                return SdfMaterialManager.GetMaterial(base.material,
                    backgroundSprite == null ? null : backgroundSprite.texture,
                    mode, shadowMode, backgroundMode);
            }
            set => base.material = value;
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            canvas.additionalShaderChannels = (AdditionalCanvasShaderChannels) int.MaxValue;
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh) {
            ApplyStyle();
            if (sprite == null) {
                mode = SdfMode.RectOnly;
            }
            if (imageType == ImageType.Sliced && hasBorder) {
                GenerateSlicedMesh(vh);
            }
            else {
                GenerateSpriteMesh(vh);
            }
        }

        private void GenerateSpriteMesh(VertexHelper vh) {
            vh.Clear();
            var rt = rectTransform;
            var spriteRect = GetNormalizedSpriteRect(sprite);
            var bgRect = GetNormalizedSpriteRect(backgroundSprite);
            var position = new Rect(
                -rt.rect.size * rt.pivot,
                rt.rect.size);
            AddRect(vh, position, spriteRect, bgRect, new Rect(Vector2.zero, Vector2.one), rt.rect.size);
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
                Vector2.one - new Vector2(borders.z / size.x, borders.w / size.y),
                Vector2.one, 
            };

            var bgRect = GetNormalizedSpriteRect(backgroundSprite);

            for (int xi = 0; xi < 3; xi++) {
                for (int yi = 0; yi < 3; yi++) {
                    var posMin = new Vector2(positionValues[xi].x, positionValues[yi].y);
                    var posSize = new Vector2(positionValues[xi + 1].x, positionValues[yi + 1].y) - posMin;
                    var positionRect = new Rect(posMin, posSize);
                    var uvMin = new Vector2(uvValues[xi].x, uvValues[yi].y);
                    var uvSize = new Vector2(uvValues[xi + 1].x, uvValues[yi + 1].y) - uvMin;
                    var uvRect = new Rect(uvMin, uvSize);
                    var coordsRect = Rect.MinMaxRect(coordsValues[xi].x, coordsValues[yi].y, coordsValues[xi + 1].x, coordsValues[yi + 1].y);
                    var localBgRect = Rect.MinMaxRect(
                        Mathf.Lerp(bgRect.xMin, bgRect.xMax, coordsRect.xMin),
                        Mathf.Lerp(bgRect.yMin, bgRect.yMax, coordsRect.yMin),
                        Mathf.Lerp(bgRect.xMin, bgRect.xMax, coordsRect.xMin),
                        Mathf.Lerp(bgRect.yMin, bgRect.yMax, coordsRect.yMax));
                    AddRect(vh, positionRect, uvRect, localBgRect, coordsRect, size);
                }
            }
        
            AddFrame(vh, 
                new Rect(startPosition, endPosition - startPosition), 
                new Rect(uvValues[0], uvValues[3]));
        }

        private Rect GetNormalizedSpriteRect(Sprite sprite) {
            if (sprite == null) return new Rect(Vector2.zero, Vector2.one);
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
            var coords = new Rect(0f, 0f, 1f, 1f);
            var grownCoords = new Rect(- ratio, 2f * ratio + Vector2.one);
        
            // A, B, C and D are left, right, bottom and top parts of the frame.
        
            var posA = new Quad2D(grownPosition.GetBottomLeft(), position.GetBottomLeft(), grownPosition.GetTopLeft(), position.GetTopLeft());
            var posB = new Quad2D(position.GetBottomRight(), grownPosition.GetBottomRight(), position.GetTopRight(), grownPosition.GetTopRight());
            var posC = new Quad2D(grownPosition.GetBottomLeft(), grownPosition.GetBottomRight(), position.GetBottomLeft(), position.GetBottomRight());
            var posD = new Quad2D(position.GetTopLeft(), position.GetTopRight(), grownPosition.GetTopLeft(), grownPosition.GetTopRight());
        
            var uvA = new Quad2D(grownUV.GetBottomLeft(), uv.GetBottomLeft(), grownUV.GetTopLeft(), uv.GetTopLeft());
            var uvB = new Quad2D(uv.GetBottomRight(), grownUV.GetBottomRight(), uv.GetTopRight(), grownUV.GetTopRight());
            var uvC = new Quad2D(grownUV.GetBottomLeft(), grownUV.GetBottomRight(), uv.GetBottomLeft(), uv.GetBottomRight());
            var uvD = new Quad2D(uv.GetTopLeft(), uv.GetTopRight(), grownUV.GetTopLeft(), grownUV.GetTopRight());
        
            var coordsA = new Quad2D(grownCoords.GetBottomLeft(), coords.GetBottomLeft(), grownCoords.GetTopLeft(), coords.GetTopLeft());
            var coordsB = new Quad2D(coords.GetBottomRight(), grownCoords.GetBottomRight(), coords.GetTopRight(), grownCoords.GetTopRight());
            var coordsC = new Quad2D(grownCoords.GetBottomLeft(), grownCoords.GetBottomRight(), coords.GetBottomLeft(), coords.GetBottomRight());
            var coordsD = new Quad2D(coords.GetTopLeft(), coords.GetTopRight(), grownCoords.GetTopLeft(), grownCoords.GetTopRight());
            
            var bgRect = new Rect(0f, 0f, 0f, 0f);
        
            AddRect(vh, posA, uvA, bgRect, coordsA, size, colorRect.LeftEdgeRect, outlineColor.LeftEdgeRect, shadowColor.LeftEdgeRect);
            AddRect(vh, posB, uvB, bgRect, coordsB, size, colorRect.RightEdgeRect, outlineColor.RightEdgeRect, shadowColor.RightEdgeRect);
            AddRect(vh, posC, uvC, bgRect, coordsC, size, colorRect.BottomEdgeRect, outlineColor.BottomEdgeRect, shadowColor.BottomEdgeRect);
            AddRect(vh, posD, uvD, bgRect, coordsD, size, colorRect.TopEdgeRect, outlineColor.TopEdgeRect, shadowColor.TopEdgeRect);
        }

        private void AddRect(VertexHelper vh, Quad2D position, Quad2D spriteRect, Quad2D bgRect, Quad2D coordsRect, Vector2 size) {
            AddRect(vh, position, spriteRect, bgRect, coordsRect, size, colorRect, outlineColor, shadowColor);
        }
        
        private void AddRect(VertexHelper vh, Quad2D position, Quad2D spriteRect, Quad2D bgRect, Quad2D coordsRect,
            Vector2 size, ColorRect colorRect, ColorRect outlineColor, ColorRect shadowColor) {
            vh.AddRect(
                position,
                spriteRect,
                bgRect,
                coordsRect,
                colorRect,
                size,
                threshold,
                rounding,
                outlineWidth, outlineColor,
                shadowColor, shadowThreshold, shadowOffset, shadowSoftness
            );
        }

        private void ApplyStyle() {
            if (_style == null) return;
            
            var size = rectTransform.rect.size;
            var minSize = Mathf.Min(size.x, size.y);
            
            // color
            colorRect = BUSSStyle.BackgroundColor.Get(Style).ColorRect;
            backgroundSprite = BUSSStyle.BackgroundImage.Get(Style).SpriteValue;
            backgroundMode = BUSSStyle.BackgroundMode.Get(Style).Enum;
            
            // outline
            outlineWidth = BUSSStyle.BorderWidth.Get(Style).FloatValue;
            outlineColor = BUSSStyle.BorderColor.Get(Style).ColorRect;
            
            // shape
            mode = BUSSStyle.SdfMode.Get(Style).Enum;
            rounding = BUSSStyle.RoundCorners.Get(Style).GetFloatValue(minSize);
            threshold = BUSSStyle.Threshold.Get(Style).FloatValue;
            sprite = BUSSStyle.SdfImage.Get(Style).SpriteValue;

            switch (BUSSStyle.BorderMode.Get(Style).Enum) {
                case BorderMode.Outside:
                    break;
                case BorderMode.Inside:
                    threshold -= outlineWidth;
                    break;
            }
            
            // shadow
            shadowColor = BUSSStyle.ShadowColor.Get(Style).ColorRect;
            shadowOffset = BUSSStyle.ShadowOffset.Get(Style).Vector2Value;
            shadowThreshold = BUSSStyle.ShadowThreshold.Get(Style).FloatValue;
            shadowSoftness = BUSSStyle.ShadowSoftness.Get(Style).FloatValue;
            shadowMode = BUSSStyle.ShadowMode.Get(Style).Enum;
            
            meshFrame = Mathf.Max(0f,
                threshold +
                Mathf.Abs(shadowThreshold) 
                + outlineWidth
                + Mathf.Max(
                    Mathf.Abs(shadowOffset.x), 
                    Mathf.Abs(shadowOffset.y)));
        }
        
        public enum ImageType {
            Simple,
            Sliced
        }

        public enum SdfMode {
            Default,
            RectOnly
        }
        
        public enum BorderMode {
            Outside,
            Inside
        }
    }
}