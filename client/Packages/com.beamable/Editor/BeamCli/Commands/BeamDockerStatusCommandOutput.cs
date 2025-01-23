
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDockerStatusCommandOutput
    {
        public bool isDaemonRunning;
        public bool isCliAccessible;
        public string cliLocation;
    }
}
