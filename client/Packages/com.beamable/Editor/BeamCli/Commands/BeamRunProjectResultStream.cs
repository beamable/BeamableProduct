
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRunProjectResultStream
    {
        public string serviceId;
        public string message;
        public float progressRatio;
    }
}
