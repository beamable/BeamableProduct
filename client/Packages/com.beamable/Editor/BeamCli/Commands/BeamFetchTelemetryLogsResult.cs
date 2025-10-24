
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamFetchTelemetryLogsResult
    {
        public System.Collections.Generic.List<BeamFetchCommandLogRecord> allLogsFound;
    }
}
