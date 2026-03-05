
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLockedFilesCheckCommandResult
    {
        public System.Collections.Generic.List<BeamProcessInfo> ProcessLockedFiles;
    }
}
