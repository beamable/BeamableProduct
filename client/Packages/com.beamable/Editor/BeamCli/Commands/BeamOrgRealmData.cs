
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamOrgRealmData
    {
        public string Cid;
        public string Pid;
        public string ParentPid;
        public string ProjectName;
        public string RealmName;
        public string RealmSecret;
        public bool IsDev;
        public bool IsStaging;
        public bool IsProduction;
    }
}
