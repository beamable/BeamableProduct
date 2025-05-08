using System.CommandLine;

namespace cli.Options;

public class IgnoreBeamoIdsOption : Option<List<string>>
{
    public static IgnoreBeamoIdsOption Instance { get; } = new IgnoreBeamoIdsOption();
    
    private IgnoreBeamoIdsOption() : base(
        name: "--ignore-beam-ids", 
        description: "A set of beam ids that should be excluded from the local " +
                     "source code scans. When a beam id is ignored, it cannot be " +
                     "deployed or understood by the CLI. The final set of ignored " +
                     "beam ids is the summation of this option AND any .beamignore files " +
                     "found in the .beamable folder")
    {
        AllowMultipleArgumentsPerToken = true;
        IsHidden = true;
        Arity = ArgumentArity.ZeroOrMore;
    }
}