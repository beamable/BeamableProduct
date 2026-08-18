
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackValidateCommandResult
    {
        public bool allOk;
        public System.Collections.Generic.List<BeamLocalStackDependencyCheck> checks;
    }
}
