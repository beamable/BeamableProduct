
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamReleasedServiceStatus
    {
        public string service;
        public bool running;
        public bool isCurrent;
        public bool ready;
        public string status;
        public string imageId;
    }
}
