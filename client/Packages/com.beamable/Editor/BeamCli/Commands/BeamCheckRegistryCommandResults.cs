
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamCheckRegistryCommandResults
    {
        public string dockerReposityName;
        public System.Collections.Generic.List<string> availableImageIds;
    }
}
