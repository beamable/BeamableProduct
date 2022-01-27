using Beamable.UI.Tweening;
using System.Collections.Generic;

namespace Beamable.UI.Buss
{
	public class BussPseudoStyle : BussStyle
	{
		public BussStyle BaseStyle;
		public bool Enabled;
		public float BlendValue;
		public FloatTween Tween;

		public BussStyle MergeWithBaseStyle(BussStyle baseStyle)
		{
			if (Enabled && BlendValue > 0f)
			{
				BaseStyle = baseStyle;
				return GetCombinedStyle();
			}

			return baseStyle;
		}
	}
}
