
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamEnvCommandResult
    {
        public System.Collections.Generic.Dictionary<string, string> environmentVariables;
        public System.Collections.Generic.Dictionary<string, string> aiDetection;
    }
}
