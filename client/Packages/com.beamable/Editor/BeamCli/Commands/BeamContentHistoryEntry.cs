
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentHistoryEntry
    {
        public string ManifestUid;
        public long CreatedDate;
        public string PublishedBy;
        public string PublishedByName;
        public string[] AffectedContentIds;
    }
}
