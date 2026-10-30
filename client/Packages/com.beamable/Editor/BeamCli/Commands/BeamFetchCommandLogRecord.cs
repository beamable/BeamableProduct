
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamFetchCommandLogRecord
    {
        public string Timestamp;
        public string LogLevel;
        public string ServiceName;
        public string Message;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Serialization", "UAC1015", Justification = "Reflection JSON requires this dictionary.")]
        public System.Collections.Generic.Dictionary<string, string> LogAttributes;
    }
}
