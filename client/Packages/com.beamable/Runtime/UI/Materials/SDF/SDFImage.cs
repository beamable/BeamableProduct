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

        public SdfMode mode;
        public ColorRect colorRect = new ColorRect(Color.white);
        public float threshold;
        public float rounding;
        public Sprite backgroundSprite;
        public SdfBackgroundMode backgroundMode;
        public float meshFrame;
        public float outlineWidth;
        public Color outlineColor;
        public Color shadowColor;
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

        protected override void OnPopulateMesh(VertexHelper vh) {
            switch (mode) {
                case SdfMode.Default:
                    break;
                case SdfMode.RectOnly:
                    break;
            }
            ApplyStyle();
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
            
            var bgRect = new Rect(0f, 0f, 0f, 0f);
        
            AddRect(vh, posA, uvA, bgRect, coordsA, size, colorRect.LeftEdgeRect);
            AddRect(vh, posB, uvB, bgRect, coordsB, size, colorRect.RightEdgeRect);
            AddRect(vh, posC, uvC, bgRect, coordsC, size, colorRect.BottomEdgeRect);
            AddRect(vh, posD, uvD, bgRect, coordsD, size, colorRect.TopEdgeRect);
        }

        private void AddRect(VertexHelper vh, Rect position, Rect spriteRect, Rect bgRect, Rect coordsRect, Vector2 size) {
            AddRect(vh, position, spriteRect, bgRect, coordsRect, size, colorRect);
        }
        
        private void AddRect(VertexHelper vh, Rect position, Rect spriteRect, Rect bgRect, Rect coordsRect, Vector2 size, ColorRect colorRect) {
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
            outlineColor = BUSSStyle.BorderColor.Get(Style).Color;
            
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
            shadowColor = BUSSStyle.ShadowColor.Get(Style).Color;
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