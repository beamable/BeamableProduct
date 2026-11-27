
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDependencyStatusInfo
    {
        public string name;
        public string kind;
        public string range;
        public bool acquired;
        public System.Nullable<long> installedVersion;
        public bool inRange;
    }
}
