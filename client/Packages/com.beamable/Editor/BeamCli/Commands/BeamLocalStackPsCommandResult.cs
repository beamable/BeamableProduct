
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackPsCommandResult
    {
        public string host;
        public string portalUrl;
        public bool backendHealthy;
        public System.Collections.Generic.List<BeamLocalStackPsEntry> steps;
    }
}
