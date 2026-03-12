
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamReplacementTypeInfo
    {
        public string ReferenceId;
        public string EngineReplacementType;
        public string EngineOptionalReplacementType;
        public string EngineImport;
    }
}
