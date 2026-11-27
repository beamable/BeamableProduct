
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamListBundlesCommandOutput
    {
        public BeamBundleSummaryInfo[] published;
        public BeamBundleInfo[] local;
    }
}
