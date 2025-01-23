
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServicesForRouteCollection
    {
        public bool knownToBeRunning;
        public string routingKey;
        public System.Collections.Generic.List<BeamServiceInstance> instances;
        public System.Collections.Generic.List<Beamable.Common.FederationInstance> federations;
    }
}
