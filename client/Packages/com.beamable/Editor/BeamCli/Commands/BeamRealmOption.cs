
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRealmOption
    {
        public string RealmName;
        public string Pid;
        public bool IsDev;
        public bool IsStaging;
        public bool IsProduction;
    }
}
