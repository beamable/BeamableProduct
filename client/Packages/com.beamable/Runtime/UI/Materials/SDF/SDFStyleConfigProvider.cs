using UnityEngine;

namespace Beamable.UI.SDF
{
    public class SDFStyleConfigProvider : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private SDFStyleConfig _config;
#pragma warning restore CS0649

        public SDFStyleConfig Config => _config;
    }
}