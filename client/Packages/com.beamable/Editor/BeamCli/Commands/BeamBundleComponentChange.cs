
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleComponentChange
    {
        public string name;
        public string kind;
        public string change;
        public string localChecksum;
        public string publishedChecksum;
    }
}
