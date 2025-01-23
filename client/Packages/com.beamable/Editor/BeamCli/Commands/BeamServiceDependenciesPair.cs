
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServiceDependenciesPair
    {
        public string name;
        public System.Collections.Generic.List<BeamDependencyData> dependencies;
    }
}
