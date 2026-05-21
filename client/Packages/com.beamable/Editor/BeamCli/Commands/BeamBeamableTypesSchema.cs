
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBeamableTypesSchema
    {
        public string GeneratedAt;
        public string AssemblyVersion;
        public BeamContentTypeEntry[] ContentTypes;
        public BeamFederationTypeEntry[] FederationTypes;
        public BeamUtilityTypeEntry[] UtilityTypes;
        public BeamUnrealTypeMappingEntry[] UnrealTypeMappings;
    }
}
