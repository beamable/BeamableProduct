
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamReportTelemetryResult
    {
        public System.Collections.Generic.List<Beamable.Common.BeamCli.Contracts.TelemetryReportStatus> AllStatus;
    }
}
