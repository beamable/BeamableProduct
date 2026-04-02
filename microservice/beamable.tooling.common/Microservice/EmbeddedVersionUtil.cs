using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beamable.Server.Common;

public static class EmbeddedVersionUtil
{
    static string _cachedPortalToolkitVersion;

    public static string GetPortalToolkitVersion()
    {
        if (_cachedPortalToolkitVersion != null)
            return _cachedPortalToolkitVersion;

        var resourceName = "beamable.tooling.common.Microservice.VersionManagement.portal-toolkit-version.json";
        var assembly = typeof(EmbeddedVersionUtil).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        var toolkitVersion = JsonSerializer.Deserialize<ToolkitVersion>(stream);
        _cachedPortalToolkitVersion = toolkitVersion.portalToolkitVersion;
        return _cachedPortalToolkitVersion;
    }
    
    [Serializable]
    public class ToolkitVersion
    {
        [JsonPropertyName("portal-toolkit-version")]
        public string portalToolkitVersion;
    }

}