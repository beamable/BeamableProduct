
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamWebPackageStatus
    {
        public string package;
        public System.Collections.Generic.List<string> localVersions;
        public string localTag;
        public string publishedAt;
    }
}
