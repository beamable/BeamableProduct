using Beamable.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private string _id;
#pragma warning restore CS0649

        public string Id => _id;

        public void ApplyStyle(BUSSStyle newStyle)
        {
            // TODO: try to avoid using SDF classes and namespaces
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                sdfImage.Style = newStyle;
            }
        }

        private void OnDisable()
        {
            // TODO: do we need this? When element will be enabled again, it will be validated and updated with new/another style
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                sdfImage.Style = null;
            }
        }
    }
}