
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamComponentExtensionOption
    {
        public string path;
        public System.Collections.Generic.List<BeamComponentSelectorOption> selectors;
    }
}
