
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInvalidTokenErrorOutput
    {
        public string refreshToken;
        public string message;
        public string invocation;
        public int exitCode;
        public string typeName;
        public string fullTypeName;
        public string stackTrace;
    }
}
