
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamLibraryEntryInfo
    {
        public string bundle;
        public bool acquired;
        public bool owned;
        public System.Nullable<System.DateTime> acquiredAt;
        public BeamLibraryInstallationInfo[] installations;
    }
}
