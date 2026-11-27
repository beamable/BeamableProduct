
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamInstallationHistoryCommandOutput
    {
        public string bundle;
        public BeamInstallationHistoryEntryInfo[] entries;
    }
}
