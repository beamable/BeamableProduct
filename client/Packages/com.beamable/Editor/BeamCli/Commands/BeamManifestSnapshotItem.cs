
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
        public Beamable.Common.BeamCli.Contracts.ManifestProjectData ProjectData;
        public long SavedTimestamp;
        public Beamable.Common.BeamCli.Contracts.ManifestAuthor Author;
        public bool IsAutoSnapshot;
        public BeamContentSnapshotListItem[] Contents;
    }
}
