
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServiceFederationChange
    {
        public string service;
        public string federationId;
        public string federationInterface;
    }
}
