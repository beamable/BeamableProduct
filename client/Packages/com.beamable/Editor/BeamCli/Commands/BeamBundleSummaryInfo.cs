
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleSummaryInfo
    {
        public string name;
        public string acl;
        public string ownerCid;
        public string latestChecksum;
        public long latestVersion;
        public Beamable.Common.BeamCli.Contracts.BundleTagInfo[] tags;
        public long updatedAt;
    }
}
