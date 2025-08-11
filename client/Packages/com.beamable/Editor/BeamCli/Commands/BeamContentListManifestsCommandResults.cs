
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentListManifestsCommandResults
    {
        public System.Collections.Generic.List<string> localManifests;
        public System.Collections.Generic.List<string> remoteManifests;
        public System.Collections.Generic.List<string> archivedManifests;
    }
}
