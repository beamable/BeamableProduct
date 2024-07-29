
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamServiceDiscoveryEvent
    {
        public string cid;
        public string pid;
        public string prefix;
        public string service;
        public bool isRunning;
        public bool isContainer;
        public string serviceType;
        public int healthPort;
        public int dataPort;
        public string executionVersion;
        public string containerId;
    }
}
