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

            _text.font = BUSSStyle.Font.Get(Style).FontAsset;
            _text.fontSize = BUSSStyle.FontSize.Get(Style).FloatValue;
            _text.color = BUSSStyle.BackgroundColor.Get(Style).ColorRect.TopLeftColor;
        }
    }
}