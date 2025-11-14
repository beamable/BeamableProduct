
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServiceStatus
    {
        public string service;
        public string serviceType;
        public string[] groups;
        public string[] storages;
        public System.Collections.Generic.List<BeamServicesForRouteCollection> availableRoutes;
    }
}
