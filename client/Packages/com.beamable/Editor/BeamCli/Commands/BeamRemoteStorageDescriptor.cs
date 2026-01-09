
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRemoteStorageDescriptor
    {
        public string storage;
        public string[] groups;
    }
}
