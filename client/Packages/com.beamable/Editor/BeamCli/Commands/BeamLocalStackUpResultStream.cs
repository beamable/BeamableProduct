
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackUpResultStream
    {
        public string step;
        public string status;
        public string message;
        public float progressRatio;
    }
}
