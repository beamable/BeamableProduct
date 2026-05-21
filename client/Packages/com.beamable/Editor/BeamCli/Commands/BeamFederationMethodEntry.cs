
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamFederationMethodEntry
    {
        public string Name;
        public string ReturnType;
        public string Summary;
        public BeamFederationMethodParam[] Parameters;
    }
}
