
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServicesBuiltProgress
    {
        public string id;
        public int totalSteps;
        public int completedSteps;
    }
}
