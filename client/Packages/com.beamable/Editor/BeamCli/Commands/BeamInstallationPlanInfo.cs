
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallationPlanInfo
    {
        public string bundle;
        public string zoneId;
        public string change;
        public BeamInstallationTargetInfo from;
        public BeamInstallationTargetInfo to;
        public BeamSitePlanInfo[] sites;
        public string[] refusals;
        public string[] warnings;
        public string[] blockers;
        public System.Nullable<bool> compatible;
        public bool autoUpdateLocked;
        public string hash;
    }
}
