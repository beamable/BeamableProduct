
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallAISkillsCommandResult
    {
        public string[] targetDirectories;
        public int installedCount;
        public int skippedCount;
        public string[] installedFiles;
        public string[] skippedFiles;
        public string fallbackMessage;
    }
}
