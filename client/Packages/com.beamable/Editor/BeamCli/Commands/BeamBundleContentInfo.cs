
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleContentInfo
    {
        public long version;
        public string checksum;
        public BeamBundleComponentInfo[] components;
        public System.Collections.Generic.Dictionary<string, string[]> componentGroups;
        public System.Collections.Generic.Dictionary<string, string> dependencies;
    }
}
