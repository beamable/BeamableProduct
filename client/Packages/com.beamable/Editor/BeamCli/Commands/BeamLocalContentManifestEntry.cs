
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalContentManifestEntry
    {
        public string FullId;
        public string TypeName;
        public string Name;
        public int CurrentStatus;
        public bool IsInConflict;
        public string Hash;
        public string[] Tags;
        public int[] TagsStatus;
        public string JsonFilePath;
        public string ReferenceManifestUid;
    }
}
