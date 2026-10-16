
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
        // JSON contract: this field is not persisted by Unity's scene/asset serializer.
#pragma warning disable UAC1015
        public System.Collections.Generic.Dictionary<string, string> LogAttributes;
#pragma warning restore UAC1015
    }
}
