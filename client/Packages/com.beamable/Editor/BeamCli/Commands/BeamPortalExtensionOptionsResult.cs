
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPortalExtensionOptionsResult
    {
        public System.Collections.Generic.List<BeamPageExtensionOption> pageExtensions;
        public System.Collections.Generic.List<BeamComponentExtensionOption> componentExtensions;
    }
}
