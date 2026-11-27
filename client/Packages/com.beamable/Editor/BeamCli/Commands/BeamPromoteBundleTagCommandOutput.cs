
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPromoteBundleTagCommandOutput
    {
        public string name;
        public Beamable.Common.BeamCli.Contracts.BundleTagInfo[] tags;
    }
}
