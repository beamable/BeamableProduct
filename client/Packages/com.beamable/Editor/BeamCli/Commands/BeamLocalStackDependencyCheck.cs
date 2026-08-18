
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackDependencyCheck
    {
        public string name;
        public bool ok;
        public string detail;
    }
}
