using System.Text.Json.Nodes;

namespace cli.Content;

/// <summary>Apply only authored changes, retaining JSON the engine's model cannot represent.</summary>
internal static class ContentPropertyEdits
{
    /// <summary>Exit code for an edit refused because the file holds data the engine's model cannot represent. Engines offer a per-item migration for it.</summary>
    public const int UnsafeEditExitCode = 7;

    /// <summary>Exit code for an edit refused because the file changed on disk after the engine loaded it.</summary>
    public const int StaleEditExitCode = 8;

    public static JsonNode Apply(JsonNode original, JsonNode model, JsonNode edited, JsonNode current)
    {
        var proposed = Preserve(original, model, edited, "properties");
        return Merge(original, proposed, current, "properties");
    }

    private static JsonNode Preserve(JsonNode original, JsonNode before, JsonNode after, string path)
    {
        if (JsonNode.DeepEquals(before, after))
        {
            return original?.DeepClone();
        }
        if (before is JsonObject oldObject && after is JsonObject newObject && original is JsonObject source)
        {
            var result = (JsonObject)source.DeepClone();
            foreach (var key in oldObject.Select(p => p.Key).Union(newObject.Select(p => p.Key)))
            {
                if (oldObject.ContainsKey(key) == newObject.ContainsKey(key) && JsonNode.DeepEquals(oldObject[key], newObject[key]))
                {
                    continue;
                }
                if (!newObject.ContainsKey(key))
                {
                    if (source.ContainsKey(key) && !JsonNode.DeepEquals(source[key], oldObject[key]))
                    {
                        throw Unsafe(path + "." + key);
                    }
                    result.Remove(key);
                }
                else
                {
                    result[key] = Preserve(source[key], oldObject[key], newObject[key], path + "." + key);
                }
            }
            return result;
        }
        // Lists have no stable element identity. Do not guess how hidden fields move after an edit.
        if (original != null && !JsonNode.DeepEquals(original, before))
        {
            throw Unsafe(path);
        }
        return after?.DeepClone();
    }

    private static JsonNode Merge(JsonNode before, JsonNode after, JsonNode current, string path)
    {
        if (JsonNode.DeepEquals(before, after))
        {
            return current?.DeepClone();
        }
        if (JsonNode.DeepEquals(current, after))
        {
            return current?.DeepClone();
        }
        if (before is JsonObject oldObject && after is JsonObject newObject && current is JsonObject source)
        {
            var result = (JsonObject)source.DeepClone();
            foreach (var key in oldObject.Select(p => p.Key).Union(newObject.Select(p => p.Key)))
            {
                if (oldObject.ContainsKey(key) == newObject.ContainsKey(key) && JsonNode.DeepEquals(oldObject[key], newObject[key]))
                {
                    continue;
                }
                if (!newObject.ContainsKey(key))
                {
                    if (source.ContainsKey(key) && !JsonNode.DeepEquals(source[key], oldObject[key]))
                    {
                        throw Stale(path + "." + key);
                    }
                    result.Remove(key);
                }
                // A missing member and an explicit null both read as null below, so compare presence first.
                else if (oldObject.ContainsKey(key) != source.ContainsKey(key) &&
                         !(source.ContainsKey(key) && JsonNode.DeepEquals(source[key], newObject[key])))
                {
                    throw Stale(path + "." + key);
                }
                else
                {
                    result[key] = Merge(oldObject[key], newObject[key], source[key], path + "." + key);
                }
            }
            return result;
        }
        if (!JsonNode.DeepEquals(current, before))
        {
            throw Stale(path);
        }
        return after?.DeepClone();
    }

    private static CliException Unsafe(string path) => new($"Cannot safely edit {path}: it contains data the local model cannot represent. Align the content model or explicitly migrate this item first.", UnsafeEditExitCode, true);
    private static CliException Stale(string path) => new($"Content changed on disk at {path}. Reload the item before saving this edit.", StaleEditExitCode, true);
}
