
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPortalExtensionUploadInfo
    {
        public string name;
        public string absolutePath;
        public string checksum;
        public System.Collections.Generic.List<BeamPortalExtensionFileUploadInfo> files;
    }
}
