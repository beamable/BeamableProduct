
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamOtelStatusResult
    {
        public int FileCount;
        public long FolderSize;
        public long LastPublishTimestamp;
    }
}
