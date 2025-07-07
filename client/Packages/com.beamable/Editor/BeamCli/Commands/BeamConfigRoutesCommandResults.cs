
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamConfigRoutesCommandResults
    {
        public string env;
        public string microserviceUri;
        public string registryUri;
        public string apiUri;
        public string portalUri;
        public string storageBrowserUri;
        public BeamConfigRoutesCommandWebsocketResult socketConfig;
    }
}
