
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentSyncProgressUpdateData
    {
        public int EventType;
        public string contentName;
        public string errorMessage;
        public int itemsToRevert;
    }
}
