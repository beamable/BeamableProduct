
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallationInfo
    {
        public string bundle;
        public string zoneId;
        public BeamInstallationTargetInfo target;
        public string status;
        public BeamBundleNoticeInfo notice;
        public BeamBundleNoticeInfo[] notices;
        public bool uninstalling;
        public bool autoUpdateLocked;
        public BeamSiteResultInfo[] sites;
        public string update;
        public BeamOverrideInfo[] overrides;
    }
}
