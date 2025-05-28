using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace beamable.tooling.common.Microservice;

/// <summary>
/// This is a collection of properties that can appear in the Beamable internal RC,
/// https://github.com/beamable/BeamableBackend/blob/main/core/src/main/scala/com/kickstand/core/RequestContext.scala#L309
/// Use the <see cref="Properties"/> property to access random properties...
///
/// Only a few fields are included by default because the other ones are not required. 
/// </summary>
public class BeamRequestContext
{
    /// <summary>
    /// The root pid of the project
    /// </summary>
    public string gameId;
    
    /// <summary>
    /// The gamerTag (also called playerId) of the player that started the request
    /// </summary>
    public long from;
    
    /// <summary>
    /// The accountId of the player that started the request
    /// </summary>
    public long accountId;

    [JsonExtensionData]
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    
    public static bool TryParse(string base64, out BeamRequestContext rc, out Exception ex)
    {
        rc = null;
        ex = null;
        try
        {
            var data = Convert.FromBase64String(base64);
            var json = Encoding.UTF8.GetString(data);
            rc = JsonSerializer.Deserialize<BeamRequestContext>(json, new JsonSerializerOptions
            {
                IncludeFields = true,
            });
        }
        catch (Exception caught)
        {
            ex = caught;
            return false;
        }

        return true;
    }
}