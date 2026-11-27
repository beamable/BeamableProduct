
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamSourceDeliveryInfo
    {
        public string id;
        public string bundle;
        public long version;
        public string[] components;
        public string connectionId;
        public string workspace;
        public string state;
        public string branch;
        public string pullRequestUrl;
        public string reviewUrl;
        public string error;
        public System.DateTime createdAt;
    }
}
