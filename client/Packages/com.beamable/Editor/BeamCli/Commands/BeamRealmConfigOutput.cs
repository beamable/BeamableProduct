
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRealmConfigOutput
    {
        // JSON contract: this field is not persisted by Unity's scene/asset serializer.
#pragma warning disable UAC1015
        public System.Collections.Generic.Dictionary<string, string> Config;
#pragma warning restore UAC1015
    }
}
