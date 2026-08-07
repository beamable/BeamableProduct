
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRemotePortalExtensionDescriptor
    {
        public string name;
        public bool enabled;
        public bool archived;
        public string[] dependencies;
    }
}
