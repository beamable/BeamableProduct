
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleComponentInfo
    {
        public string name;
        public string kind;
        public string site;
        public bool source;
    }
}
