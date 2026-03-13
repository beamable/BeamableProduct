
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentHistoryCommandEvent
    {
        public int EventType;
        public BeamContentHistoryEntriesPage EntriesPage;
        public BeamContentHistoryChangelistPage ChangelistsPage;
        public System.Collections.Generic.List<string> EntriesToRemove;
        public System.Collections.Generic.List<string> ChangelistsToRemove;
    }
}
