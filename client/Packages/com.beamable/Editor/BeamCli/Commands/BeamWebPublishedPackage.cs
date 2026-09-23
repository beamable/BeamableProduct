
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamWebPublishedPackage
    {
        public string package;
        public string sourceVersion;
        public string publishedVersion;
    }
}
