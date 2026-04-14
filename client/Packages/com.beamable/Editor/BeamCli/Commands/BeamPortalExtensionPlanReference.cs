
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPortalExtensionPlanReference
    {
        public string name;
        public string checksum;
        public bool enabled;
        public bool archived;
        public System.Collections.Generic.List<BeamPortalExtensionPlanFileReference> files;
    }
}
