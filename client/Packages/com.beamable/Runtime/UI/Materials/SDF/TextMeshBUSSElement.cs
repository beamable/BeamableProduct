using Beamable.UI.BUSS;
using TMPro;
using UnityEngine;

namespace Beamable.UI.SDF {
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
    public class TextMeshBUSSElement : BUSSElement {
        private TextMeshProUGUI _text;
        private bool _hasText;

        // public override void NotifyOnStyleChanged(BUSSStyle newStyle) {
        //     if (!_hasText) {
        //         _text = GetComponent<TextMeshProUGUI>();
        //         _hasText = true;
        //     }
        //
        //     if (newStyle == null) return;
        //
        //     _text.font = BUSSStyle.Font.Get(newStyle).FontAsset;
        //     _text.fontSize = BUSSStyle.FontSize.Get(newStyle).FloatValue;
        //     _text.color = BUSSStyle.BackgroundColor.Get(newStyle).ColorRect.TopLeftColor;
        // }
    }
}