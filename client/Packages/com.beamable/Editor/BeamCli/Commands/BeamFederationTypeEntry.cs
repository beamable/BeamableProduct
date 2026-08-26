
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamFederationTypeEntry
    {
        public string InterfaceName;
        public string Namespace;
        public string Summary;
        public string GenericConstraint;
        public string Platform;
        public BeamFederationMethodEntry[] Methods;
    }
}
