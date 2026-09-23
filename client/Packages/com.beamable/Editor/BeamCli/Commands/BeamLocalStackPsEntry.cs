
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLocalStackPsEntry
    {
        public string name;
        public string group;
        public int pid;
        public string kind;
        public bool running;
        public string logPath;
    }
}
