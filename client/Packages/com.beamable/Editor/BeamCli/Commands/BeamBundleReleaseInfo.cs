
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleReleaseInfo
    {
        public long version;
        public string checksum;
        public long publishedAt;
    }
}
