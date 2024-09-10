
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamGetTokenForGuestCommandOutput
    {
        public string accessToken;
        public string challengeToken;
        public long expiresIn;
        public string refreshToken;
        public string[] scopes;
        public string tokenType;
    }
}
