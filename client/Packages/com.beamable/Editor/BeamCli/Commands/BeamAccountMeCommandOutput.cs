
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamAccountMeCommandOutput
    {
        public long id;
        public string email;
        public string language;
        public System.Collections.Generic.List<string> scopes;
        public System.Collections.Generic.List<string> thirdPartyAppAssociations;
        public System.Collections.Generic.List<string> deviceIds;
        public System.Collections.Generic.List<BeamAccountMeExternalIdentity> external;
    }
}
