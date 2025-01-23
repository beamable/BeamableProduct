
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamClearTempLogFilesCommandOutput
    {
        public System.Collections.Generic.List<string> deletedFiles;
        public System.Collections.Generic.List<string> failedToDeleteFiles;
    }
}
