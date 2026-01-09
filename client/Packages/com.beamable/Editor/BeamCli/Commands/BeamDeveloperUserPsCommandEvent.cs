
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDeveloperUserPsCommandEvent
    {
        public int EventType;
        public BeamDeveloperUserResult DeveloperUserReport;
    }
}
