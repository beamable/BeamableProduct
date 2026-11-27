
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamSearchMarketplaceCommandOutput
    {
        public BeamBundleSummaryInfo[] bundles;
        public string nextCursor;
    }
}
