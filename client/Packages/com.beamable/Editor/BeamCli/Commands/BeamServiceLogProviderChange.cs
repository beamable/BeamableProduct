
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServiceLogProviderChange
    {
        public string service;
        public string oldProvider;
        public string newProvider;
    }
}
