
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLibraryInstallationInfo
    {
        public string zoneId;
        public long version;
        public string status;
        public BeamBundleNoticeInfo notice;
        public BeamBundleNoticeInfo[] notices;
        public bool uninstalling;
        public string update;
    }
}
