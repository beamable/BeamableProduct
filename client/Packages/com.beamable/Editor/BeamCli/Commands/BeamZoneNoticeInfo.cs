
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamZoneNoticeInfo
    {
        public string zoneId;
        public string bundle;
        public long version;
        public string status;
        public BeamBundleNoticeInfo notice;
        public System.Nullable<long> replacementVersion;
        public string update;
    }
}
