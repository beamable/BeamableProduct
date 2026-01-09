
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentPsProgressMessage
    {
        public int total;
        public int completed;
        public string message;
    }
}
