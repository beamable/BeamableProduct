
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamWebPublishCommandResults
    {
        public string registry;
        public string version;
        public System.Collections.Generic.List<BeamWebPublishedPackage> published;
    }
}
