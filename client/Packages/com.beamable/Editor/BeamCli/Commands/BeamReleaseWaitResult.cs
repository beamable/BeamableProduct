
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamReleaseWaitResult
    {
        public bool allReady;
        public bool timedOut;
        public double timeoutSeconds;
        public double elapsedSeconds;
        public System.Collections.Generic.List<BeamReleasedServiceStatus> services;
    }
}
