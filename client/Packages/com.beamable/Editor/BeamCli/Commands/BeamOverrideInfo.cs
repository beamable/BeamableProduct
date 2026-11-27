
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamOverrideInfo
    {
        public string site;
        public string component;
        public string connectionId;
        public long baseVersion;
        public System.Nullable<long> changedIn;
    }
}
