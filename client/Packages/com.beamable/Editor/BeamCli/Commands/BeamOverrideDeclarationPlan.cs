
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamOverrideDeclarationPlan
    {
        public string component;
        public string bundle;
        public long version;
        public string kind;
        public string connectionId;
        public string remote;
    }
}
