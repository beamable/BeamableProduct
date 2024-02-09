
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamServiceRemoteDeployProgressResult
    {
        public string BeamoId;
        public double BuildAndTestProgress;
        public double ContainerUploadProgress;
    }
}
