
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamMcpStatusResult
    {
        public bool available;
        public string url;
        public string version;
        public int pid;
        public System.Collections.Generic.List<string> protocolVersions;
        public string reason;
    }
}
