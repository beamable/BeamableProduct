
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackInitCommandResult
    {
        public string manifestPath;
        public int stepCount;
        public bool created;
    }
}
