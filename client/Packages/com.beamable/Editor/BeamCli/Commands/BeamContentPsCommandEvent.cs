
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentPsCommandEvent
    {
        public int EventType;
        public System.Collections.Generic.List<BeamLocalContentManifest> RelevantManifestsAgainstLatest;
        public System.Collections.Generic.List<BeamLocalContentManifest> ToRemoveLocalEntries;
        public string PublisherEmail;
        public string PublisherAccountId;
        public System.Collections.Generic.List<BeamContentSyncReport> SyncReports;
    }
}
