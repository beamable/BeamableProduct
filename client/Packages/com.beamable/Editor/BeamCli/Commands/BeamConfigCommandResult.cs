
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamConfigCommandResult
    {
        public string host;
        public string cid;
        public string pid;
        public string configPath;
    }
}
