
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentHistoryEntriesPage
    {
        public string[] ContainedManifestUids;
        public long StartDate;
        public long EndDate;
        public System.Collections.Generic.List<BeamContentHistoryEntry> Entries;
    }
}
