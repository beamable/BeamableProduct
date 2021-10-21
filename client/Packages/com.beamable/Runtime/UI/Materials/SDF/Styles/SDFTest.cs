using System;
using Beamable.Editor.UI.SDF;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    public class SDFTest : MonoBehaviour {
        [SerializableValueImplements(typeof(IVertexColorProperty))]
        public SerializableValueObject backgroundColor;
    }
}