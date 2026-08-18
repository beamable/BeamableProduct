
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPortalExtensionUpdateToolkitCommandResults
    {
        public string version;
        public System.Collections.Generic.List<string> updated;
        public System.Collections.Generic.List<string> skipped;
    }
}
