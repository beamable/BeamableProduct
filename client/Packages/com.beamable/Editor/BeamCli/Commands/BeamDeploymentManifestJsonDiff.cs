
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDeploymentManifestJsonDiff
    {
        public string jsonPath;
        public string type;
        public string currentValue;
        public string nextValue;
    }
}
