using TMPro;
using UnityEngine;

namespace Beamable.UI.Buss
{
	[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
	public class TextMeshBussElement : BussElement
	{
		private TextMeshProUGUI _text;
		private bool _hasText;
		private bool _hasTMPEssentials;

		public override void ApplyStyle()
		{
			if (!_hasText)
			{
				_text = GetComponent<TextMeshProUGUI>();
				_hasText = true;
			}

			if (Style == null) return;

			// BASE
			_text.font = BussStyle.Font.Get(Style).FontAsset;
			_text.fontSize = BussStyle.FontSize.Get(Style).FloatValue;
			_text.color = BussStyle.FontColor.Get(Style).Color;

			// Alignment
			_text.alignment = BussStyle.TextAlignment.Get(Style).Enum;
		}
	}
}
