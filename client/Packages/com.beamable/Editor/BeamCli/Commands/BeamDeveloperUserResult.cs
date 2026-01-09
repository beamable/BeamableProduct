
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDeveloperUserResult
    {
        public System.Collections.Generic.List<BeamDeveloperUserData> CreatedUsers;
        public System.Collections.Generic.List<BeamDeveloperUserData> DeletedUsers;
        public System.Collections.Generic.List<BeamDeveloperUserData> UpdatedUsers;
        public System.Collections.Generic.List<BeamDeveloperUserData> SavedUsers;
    }
}
