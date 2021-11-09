using TMPro;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
    public class TextMeshBUSSElement : BUSSElement
    {
        private TextMeshProUGUI _text;
        private bool _hasText;

        public override void ApplyStyle()
        {
            if (!_hasText)
            {
                _text = GetComponent<TextMeshProUGUI>();
                _hasText = true;
            }

            if (Style == null) return;

            // BASE

            _text.font = BUSSStyle.Font.Get(Style).FontAsset;
            _text.fontSize = BUSSStyle.FontSize.Get(Style).FloatValue;
            _text.color = BUSSStyle.BackgroundColor.Get(Style).ColorRect.TopLeftColor;

            // OUTLINE

            float borderWidth = BUSSStyle.BorderWidth.Get(Style).FloatValue;

            if (borderWidth > 0)
                _text.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            else
                _text.fontMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);

            _text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, borderWidth);
            _text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, BUSSStyle.BorderColor.Get(Style).ColorRect.TopLeftColor);
            _text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, BUSSStyle.ShadowColor.Get(Style).ColorRect.TopLeftColor);

            // SHADOW

            Vector2 shadowOffset = BUSSStyle.ShadowOffset.Get(Style).Vector2Value;

            if (shadowOffset != Vector2.zero)
                _text.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            else
                _text.fontMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);

            _text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, shadowOffset.x);
            _text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, shadowOffset.y);
            _text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, BUSSStyle.ShadowSoftness.Get(Style).FloatValue);
            _text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, BUSSStyle.ShadowThreshold.Get(Style).FloatValue);

        }
    }
}