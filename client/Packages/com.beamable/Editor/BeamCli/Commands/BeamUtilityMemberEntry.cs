
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamUtilityMemberEntry
    {
        public string Name;
        public string Type;
        public bool IsStatic;
        public string MemberKind;
        public string Summary;
    }
}
