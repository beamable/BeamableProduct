
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamServiceInstance
    {
        public long startedByAccountId;
        public string startedByAccountEmail;
        public string primaryKey;
        public BeamDockerServiceDescriptor latestDockerEvent;
        public BeamHostServiceDescriptor latestHostEvent;
        public BeamRemoteServiceDescriptor latestRemoteEvent;
        public BeamRemoteStorageDescriptor latestRemoteStorageEvent;
    }
}
