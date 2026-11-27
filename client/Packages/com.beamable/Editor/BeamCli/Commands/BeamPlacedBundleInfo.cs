
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPlacedBundleInfo
    {
        public string place;
        public string bundle;
        public long latestVersion;
        public bool acquired;
        public bool owned;
        public string[] componentGroups;
        public string installation;
    }
}
