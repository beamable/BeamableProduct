
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDeveloperUserData
    {
        public long GamerTag;
        public long TemplateGamerTag;
        public bool IsCorrupted;
        public int DeveloperUserType;
        public string Alias;
        public string Description;
        public System.Collections.Generic.List<string> Tags;
        public string AccessToken;
        public string RefreshToken;
        public string Pid;
        public string Cid;
        public long ExpiresIn;
        public long IssuedAt;
        public long CreatedDate;
    }
}
