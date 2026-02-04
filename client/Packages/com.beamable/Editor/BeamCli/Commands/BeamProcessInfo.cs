
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamProcessInfo
    {
        public int ProcessId;
        public string CommandLine;
        public System.Collections.Generic.List<string> LockingFiles;
    }
}
