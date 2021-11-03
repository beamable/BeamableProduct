using Beamable.UI.SDF;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SDFImage))]
    public class SdfImageBUSSElement : BUSSElement 
    {
        private SDFImage _image;
        private bool _hasImage;

        public override void ApplyStyle() 
        {
            if (!_hasImage) 
            {
                _image = GetComponent<SDFImage>();
                _hasImage = true;
            }
        
            _image.Style = Style;
        }
    }
}