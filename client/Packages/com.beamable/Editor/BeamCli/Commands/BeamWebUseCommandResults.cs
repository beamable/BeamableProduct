
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamWebUseCommandResults
    {
        public string version;
        public System.Collections.Generic.List<BeamWebUsedProject> updated;
        public System.Collections.Generic.List<string> skipped;
        public System.Collections.Generic.List<string> failed;
    }
}
