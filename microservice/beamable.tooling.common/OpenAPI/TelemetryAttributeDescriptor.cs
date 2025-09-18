using Beamable.Server;

namespace Beamable.Tooling.Common.OpenAPI;

public struct TelemetryAttributeDescriptor
{
    public string name;
    public string description;
    public TelemetryImportance level;
    public TelemetryAttributeType type;
    public TelemetryAttributeSource source;
}

public static class TelemetryAttributeExtensions
{
    public static TelemetryAttributeDescriptor FromAttribute(this TelemetryAttribute attr, TelemetryAttributeSource source)
    {
        return new TelemetryAttributeDescriptor
        {
            name = attr.name,
            description = attr.description,
            level = attr.level,
            type = attr.type,
            source = source
        };
    }
}