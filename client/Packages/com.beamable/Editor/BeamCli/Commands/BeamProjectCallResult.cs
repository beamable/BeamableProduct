
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamProjectCallResult
    {
        public string service;
        public string method;
        public string target;
        public string routingKey;
        public string url;
        public int status;
        public bool success;
        public string body;
        public long playerId;
        public bool createdGuest;
        public string refreshToken;
    }
}
