
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamGetTokenListCommandOutput
    {
        public int itemCount;
        public System.Collections.Generic.List<BeamGetTokenListElement> items;
    }
}