
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamEndOverrideCommandOutput
    {
        public string bundle;
        public string component;
        public string manifestId;
        public string[] warnings;
    }
}
