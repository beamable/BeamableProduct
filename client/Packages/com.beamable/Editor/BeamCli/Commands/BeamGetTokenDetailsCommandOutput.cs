
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGetTokenDetailsCommandOutput
    {
        public bool wasRefreshToken;
        public long accountId;
        public long cid;
        public long created;
        public string device;
        public long expiresMs;
        public long gamerTag;
        public string pid;
        public string platform;
        public bool revoked;
        public string[] scopes;
        public string token;
        public string type;
    }
}
