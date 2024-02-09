
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamTailLogMessageForClient
    {
        public string raw;
        public string logLevel;
        public string message;
        public string timeStamp;
    }
}
