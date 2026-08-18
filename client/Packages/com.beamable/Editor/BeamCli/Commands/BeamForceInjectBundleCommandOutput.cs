
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamForceInjectBundleCommandOutput
    {
        public string bundleName;
        public string cid;
        public string realmId;
        public string checksum;
        public bool removed;
    }
}
