
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamSetEnabledCommandComponent
    {
        public string service;
        public string enabled;
    }
}
