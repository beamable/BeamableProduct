
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamManifestSnapshotItem
    {
        public string Name;
        public string Path;
        public string ManifestId;
        public string Pid;
        public long SavedTimestamp;
        public BeamContentSnapshotListItem[] Contents;
    }
}
