
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallationTargetInfo
    {
        public long version;
        public string[] componentGroups;
        public string realms;
        public bool autoUpdate;
    }
}
