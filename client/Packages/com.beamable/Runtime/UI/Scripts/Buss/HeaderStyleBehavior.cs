using System;
using Beamable.UI.MSDF;
using Beamable.UI.Buss.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.UI.Buss
{
    public class HeaderStyleBehaviour : StyleBehaviour
    {
        static HeaderStyleBehaviour()
        {
            RegisterType<HeaderStyleBehaviour>("header");
        }

        public override string TypeString => "header";
        
        public BeamableMSDFBehaviour MsdfBehaviour;

        public void Update()
        {
            // Do the update things
        }

        public override void Apply(StyleObject styles)
        {
            MsdfBehaviour.ApplyStyleObject(styles);
        }
    }
}