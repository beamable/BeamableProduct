
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentPsCommandEvent
    {
        public int EventType;
        public System.Collections.Generic.List<Beamable.Common.BeamCli.Contracts.LocalContentManifest> RelevantManifestsAgainstLatest;
        public System.Collections.Generic.List<Beamable.Common.BeamCli.Contracts.LocalContentManifest> ToRemoveLocalEntries;
        public string PublisherEmail;
        public string PublisherAccountId;
        public System.Collections.Generic.List<BeamContentSyncReport> SyncReports;
    }
}
