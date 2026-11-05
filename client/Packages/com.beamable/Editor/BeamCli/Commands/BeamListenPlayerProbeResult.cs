
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamListenPlayerProbeResult
    {
        public bool success;
        public string message;
        public string failedStage;
        public string identity;
        public string provider;
        public string socketUri;
        public int handshakeStatusCode;
        public bool opened;
        public bool connectTimedOut;
        public double connectTimeoutSeconds;
        public long timeToOpenMs;
        public bool sessionStartSent;
        public string sessionStartFrame;
        public double listenSeconds;
        public int framesReceived;
        public System.Collections.Generic.List<string> frames;
        public bool closedByServer;
        public long closedAfterMs;
        public int closeStatusCode;
        public string closeStatusDescription;
        public string error;
    }
}
