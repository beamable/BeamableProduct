
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamAccountMeExternalIdentity
    {
        public string providerNamespace;
        public string providerService;
        public string userId;
    }
}
