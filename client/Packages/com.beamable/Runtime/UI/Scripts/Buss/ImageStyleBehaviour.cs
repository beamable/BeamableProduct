using Beamable.UI.MSDF;
using Beamable.UI.Buss.Extensions;
using UnityEngine;

namespace Beamable.UI.Buss
{
    [ExecuteInEditMode]
    public class ImageStyleBehaviour : StyleBehaviour
    {
        static ImageStyleBehaviour()
        {
            RegisterType<ImageStyleBehaviour>("img");
        }

        public BeamableMSDFBehaviour MsdfBehaviour;

        public override string TypeString => "img";

        public override void Apply(StyleObject styles)
        {
            MsdfBehaviour.ApplyStyleObject(styles);

        }
    }
}
