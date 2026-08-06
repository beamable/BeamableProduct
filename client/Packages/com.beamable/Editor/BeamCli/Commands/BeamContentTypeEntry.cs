
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentTypeEntry
    {
        public string TypeName;
        public string ClassName;
        public string Namespace;
        public string Summary;
        public string Platform;
        public string[] FormerlyKnownAs;
        public BeamContentFieldEntry[] Fields;
    }
}
