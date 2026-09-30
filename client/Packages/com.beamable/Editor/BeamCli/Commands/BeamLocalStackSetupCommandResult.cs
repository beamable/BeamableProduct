
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackSetupCommandResult
    {
        public string toolchainDir;
        public string manifestPath;
        public bool allOk;
        public System.Collections.Generic.List<BeamLocalStackSetupStepResult> steps;
    }
}
