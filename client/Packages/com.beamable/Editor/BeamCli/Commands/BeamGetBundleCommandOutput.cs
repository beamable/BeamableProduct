
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGetBundleCommandOutput
    {
        public BeamBundleInfo bundle;
        public Beamable.Common.BeamCli.Contracts.BundleTagInfo[] tags;
    }
}
