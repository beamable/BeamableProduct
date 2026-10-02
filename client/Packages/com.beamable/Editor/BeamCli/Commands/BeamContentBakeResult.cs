
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentBakeResult
    {
        public string ContentPath;
        public string ManifestPath;
        public int Count;
    }
}
