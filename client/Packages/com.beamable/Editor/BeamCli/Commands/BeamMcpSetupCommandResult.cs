
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamMcpSetupCommandResult
    {
        public string configPath;
        public string command;
        public string[] args;
        public bool beamResolves;
        public System.Collections.Generic.List<string> warnings;
    }
}
