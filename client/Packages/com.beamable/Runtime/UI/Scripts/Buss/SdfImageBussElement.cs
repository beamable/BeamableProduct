using Beamable.UI.Sdf;
using UnityEngine;

namespace Beamable.UI.Buss
{
	[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SdfImage))]
	public class SdfImageBussElement : BussElement
	{
		private SdfImage _image;
		private bool _hasImage;

		public override string TypeName => "div";

		public override void ApplyStyle()
		{
			if (!_hasImage)
			{
				_image = GetComponent<SdfImage>();
				_hasImage = true;
			}

			var combined = Style.GetCombinedStyle();
			base.ApplyRectStyle(combined);
			
			_image.Style = combined;
		}
	}
}
