
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamMountSiteConfig
    {
        public string path;
        public System.Collections.Generic.List<BeamMountSiteSelector> selectors;
        public System.Collections.Generic.List<string> navContext;
    }
}
