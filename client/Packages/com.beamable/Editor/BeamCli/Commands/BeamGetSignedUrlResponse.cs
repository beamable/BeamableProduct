
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGetSignedUrlResponse
    {
        public System.Collections.Generic.List<BeamGetLogsUrlHeader> headers;
        public string url;
        public string body;
        public string method;
    }
}
