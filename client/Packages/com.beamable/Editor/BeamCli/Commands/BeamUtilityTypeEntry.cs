
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamUtilityTypeEntry
    {
        public string TypeName;
        public string Namespace;
        public string Kind;
        public string Summary;
        public string Platform;
        public BeamUtilityMemberEntry[] Members;
        public BeamUtilityMethodEntry[] Methods;
        public string[] EnumValues;
    }
}
