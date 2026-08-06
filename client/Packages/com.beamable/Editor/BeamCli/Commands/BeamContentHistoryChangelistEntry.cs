
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentHistoryChangelistEntry
    {
        public string OldVersion;
        public string OldChecksum;
        public string[] OldTags;
        public string NewVersion;
        public string NewChecksum;
        public string[] NewTags;
        public string JsonFilePath;
        public string FullId;
        public string TypeName;
        public string Name;
        public int ChangeStatus;
        public long ChangeDate;
    }
}
