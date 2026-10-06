
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRealmCheckEntry
    {
        public string id;
        public string status;
        public string message;
        public string fixCommand;
    }
}
