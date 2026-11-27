
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleNoticeCommandOutput
    {
        public string name;
        public BeamBundleNoticeInfo notice;
        public long installationsOutsideOrg;
    }
}
