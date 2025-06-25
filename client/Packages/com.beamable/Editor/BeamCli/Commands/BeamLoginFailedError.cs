
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLoginFailedError
    {
        public string message;
        public string invocation;
        public int exitCode;
        public string typeName;
        public string fullTypeName;
        public string stackTrace;
    }
}
