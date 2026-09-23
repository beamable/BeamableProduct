
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRealmCheckResult
    {
        public string target;
        public bool ok;
        public System.Collections.Generic.List<BeamRealmCheckEntry> checks;
    }
}
