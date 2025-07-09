
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentProgressUpdateData
    {
        public int EventType;
        public string contentName;
        public string errorMessage;
        public int totalItems;
        public int processedItems;
    }
}
