
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRealmConfigUseCommandResult
    {
        public string pid;
        public string realmName;
        public string gameName;
        public string gamePid;
    }
}
