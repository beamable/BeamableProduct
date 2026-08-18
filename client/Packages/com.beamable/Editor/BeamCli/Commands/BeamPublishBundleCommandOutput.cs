
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPublishBundleCommandOutput
    {
        public string name;
        public string checksum;
        public bool isNew;
        public BeamBundleDiffResult diff;
    }
}
