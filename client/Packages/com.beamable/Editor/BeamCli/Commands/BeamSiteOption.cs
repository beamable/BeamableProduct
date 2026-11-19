
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamSiteOption
    {
        public string id;
        public string presentation;
        public string description;
        public System.Collections.Generic.List<string> renderedBy;
    }
}
