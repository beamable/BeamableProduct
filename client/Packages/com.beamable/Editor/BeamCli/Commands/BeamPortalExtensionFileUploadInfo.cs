
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamPortalExtensionFileUploadInfo
    {
        public string fileName;
        public string contentType;
        public string checksum;
        public bool needsUpload;
        public string existingContentId;
    }
}
