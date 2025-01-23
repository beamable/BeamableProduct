
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServerDescriptor
    {
        public int port;
        public int pid;
        public long inflightRequests;
        public string url;
        public string owner;
        public string version;
    }
}
