
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamTelemetryReportStatus
    {
        public string FilePath;
        public bool Success;
        public string ErrorMessage;
    }
}
