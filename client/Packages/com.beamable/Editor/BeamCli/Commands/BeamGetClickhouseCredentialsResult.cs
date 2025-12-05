
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGetClickhouseCredentialsResult
    {
        public string endpoint;
        public Beamable.Common.Content.OptionalDateTime expiresAt;
        public string password;
        public string username;
    }
}
