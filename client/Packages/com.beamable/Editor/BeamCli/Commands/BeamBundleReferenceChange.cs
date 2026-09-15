
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleReferenceChange
    {
        public string bundle;
        public string oldChecksum;
        public string nextChecksum;
    }
}
