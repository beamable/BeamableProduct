
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamUnrealTypeMappingEntry
    {
        public string CppType;
        public string CSharpEquivalent;
        public string Notes;
    }
}
