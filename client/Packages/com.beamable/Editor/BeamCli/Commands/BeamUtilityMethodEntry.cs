
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamUtilityMethodEntry
    {
        public string Name;
        public string ReturnType;
        public bool IsStatic;
        public string Summary;
        public BeamUtilityParam[] Parameters;
    }
}
