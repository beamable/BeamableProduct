
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamAccountMeCommandOutput
    {
        public long id;
        public string email;
        public string language;
        public System.Collections.Generic.List<string> scopes;
        public System.Collections.Generic.List<string> thirdPartyAppAssociations;
        public System.Collections.Generic.List<string> deviceIds;
        public System.Collections.Generic.List<BeamAccountMeExternalIdentity> external;
        public string roleString;
        public System.Collections.Generic.List<BeamRealmRole> roles;
        public string tokenCid;
        public string tokenPid;
        public string accessToken;
        public string refreshToken;
        public System.DateTime tokenExpiration;
        public System.DateTime tokenIssuedAt;
        public long tokenExpiresIn;
    }
}
