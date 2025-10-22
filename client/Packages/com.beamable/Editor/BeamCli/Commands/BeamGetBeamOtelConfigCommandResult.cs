
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGetBeamOtelConfigCommandResult
    {
        public string BeamCliTelemetryLogLevel;
        public long BeamCliTelemetryMaxSize;
        public bool BeamCliAllowTelemetry;
    }
}
