
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallBundleCommandOutput
    {
        public BeamInstallationPlanInfo plan;
        public bool applied;
        public BeamSiteResultInfo[] results;
        public string[] warnings;
    }
}
