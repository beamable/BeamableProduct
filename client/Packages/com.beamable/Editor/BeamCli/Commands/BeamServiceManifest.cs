
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamServiceManifest
    {
        public string id;
        public long created;
        public System.Collections.Generic.List<BeamServiceReference> manifest;
        public System.Collections.Generic.List<BeamServiceStorageReference> storageReference;
        public long createdByAccountId;
        public string comments;
    }
}
