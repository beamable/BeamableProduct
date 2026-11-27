
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleReleasesCommandOutput
    {
        public string name;
        public BeamBundleReleaseInfo[] releases;
        public Beamable.Common.BeamCli.Contracts.BundleTagInfo[] tags;
        public BeamBundleNoticeInfo[] notices;
        public BeamBundleContentInfo content;
    }
}
