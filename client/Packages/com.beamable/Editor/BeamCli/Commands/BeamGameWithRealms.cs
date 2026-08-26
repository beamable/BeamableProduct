
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGameWithRealms
    {
        public string GameName;
        public string Pid;
        public BeamRealmOption[] Realms;
    }
}
