
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundlePlanCommandOutput
    {
        public string name;
        public bool isNoOp;
        public string publishedChecksum;
        public System.Collections.Generic.List<BeamBundleComponentChange> changes;
        public string planPath;
    }
}
