
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallationHistoryEntryInfo
    {
        public System.DateTime at;
        public string change;
        public string actor;
        public BeamInstallationTargetInfo from;
        public BeamInstallationTargetInfo to;
        public BeamSiteResultInfo[] results;
    }
}
