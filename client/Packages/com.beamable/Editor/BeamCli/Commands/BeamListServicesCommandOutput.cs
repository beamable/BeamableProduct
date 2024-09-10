
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamListServicesCommandOutput
    {
        public string cid;
        public string pid;
        public System.Collections.Generic.List<BeamRunningService> services;
    }
}
