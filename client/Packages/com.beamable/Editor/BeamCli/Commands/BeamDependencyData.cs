
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamDependencyData
    {
        public string name;
        public string projPath;
        public string dllName;
        public string type;
    }
}
