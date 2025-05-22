
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamCheckGrafanaCommandResults
    {
        public bool isRunning;
        public string url;
        public string containerName;
    }
}
