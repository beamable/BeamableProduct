
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackSetupStepResult
    {
        public string name;
        public string status;
        public string detail;
        public bool ok;
    }
}
