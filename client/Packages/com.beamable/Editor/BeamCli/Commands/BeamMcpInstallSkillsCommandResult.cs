
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamMcpInstallSkillsCommandResult
    {
        public string targetDirectory;
        public int installedCount;
        public int skippedCount;
        public string[] installedFiles;
        public string[] skippedFiles;
    }
}
