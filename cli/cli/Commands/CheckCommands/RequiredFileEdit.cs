using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace cli.CheckCommands;

public delegate RequiredFileEdit ProjectFileEditFunction(string beamoId, Dictionary<int, int> lineNumberToIndex,
    Project project);

public class FileCache : Dictionary<string, string>
{
    
}

public class RequiredFileEdit
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

    public bool TrySetLocation(ElementLocation location, Dictionary<int, int> lineNumberToIndex, string originalText)
    {
        if (!lineNumberToIndex.TryGetValue(location.Line - 1, out var index))
        {
            return false;
        }

        startIndex = index + location.Column;
        this.originalText = originalText;
        endIndex = startIndex + originalText.Length ;
        line = location.Line;
        column = location.Column;
        return true;
    }

    public string Apply(string fullText)
    {
        var before = fullText.Substring(0, startIndex);
        var after = fullText.Substring(endIndex);
        var newText = before + replacementText + after;
        
        File.WriteAllText(filePath, newText);
        return newText;
    }
}