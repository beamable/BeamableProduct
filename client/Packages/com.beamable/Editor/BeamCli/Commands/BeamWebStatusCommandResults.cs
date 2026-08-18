
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamWebStatusCommandResults
    {
        public string registry;
        public string cdn;
        public bool registryReachable;
        public bool cdnReachable;
        public System.Collections.Generic.List<BeamWebPackageStatus> packages;
    }
}
