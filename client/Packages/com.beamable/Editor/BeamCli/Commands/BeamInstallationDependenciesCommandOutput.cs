
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallationDependenciesCommandOutput
    {
        public string name;
        public long version;
        public BeamDependencyStatusInfo[] dependencies;
    }
}
