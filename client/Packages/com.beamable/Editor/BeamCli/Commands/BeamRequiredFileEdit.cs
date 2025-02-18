
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRequiredFileEdit
    {
        public string code;
        public string beamoId;
        public string title;
        public string description;
        public string filePath;
        public string replacementText;
        public string originalText;
        public int startIndex;
        public int endIndex;
        public int line;
        public int column;
    }
}
