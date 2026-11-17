using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beamable.Serialization.SmallerJSON;

namespace Beamable.Editor.UI.ContentWindow
{
    // Presentation only. The service's immutable preview remains the authority for applying changes.
    internal sealed class ContentSchemaPreview
    {
        internal enum Change { Removed, Added, Changed, Unchanged }
        internal sealed class Row
        {
            internal string Path, Before, After;
            internal Change Kind;
        }

        internal readonly List<Row> Rows = new();
        internal readonly string CurrentJson, ProposedJson;

        internal ContentSchemaPreview(string originalJson, string proposedProperties)
        {
            if (!(Json.Deserialize(originalJson) is ArrayDict root) ||
                !root.TryGetValue("properties", out var properties) || !(properties is ArrayDict before) ||
                !(Json.Deserialize(proposedProperties) is ArrayDict after))
                throw new InvalidOperationException("Unable to compare content properties. Refresh and review this item again.");
            CurrentJson = Json.FormatJson(Serialize(before));
            ProposedJson = Json.FormatJson(Serialize(after));
            foreach (var key in before.Keys.Union(after.Keys).OrderBy(k => k, StringComparer.Ordinal))
            {
                var hasBefore = before.TryGetValue(key, out var oldValue);
                var hasAfter = after.TryGetValue(key, out var newValue);
                // Content properties wrap ordinary values in 'data'; links keep their explicit wire keys.
                Compare("/" + Escape(key), Unwrap(oldValue), hasBefore, Unwrap(newValue), hasAfter);
            }
        }

        private static object Unwrap(object value) => value is ArrayDict dict && dict.Count == 1 &&
            dict.TryGetValue("data", out var data) ? data : value;
        private static string Escape(string key) => key.Replace("~", "~0").Replace("/", "~1");
        private static string Serialize(object value) => Json.Serialize(value, new StringBuilder());

        private void Compare(string path, object before, bool hasBefore, object after, bool hasAfter)
        {
            if (hasBefore && hasAfter && before is ArrayDict oldObject && after is ArrayDict newObject &&
                (oldObject.Count > 0 || newObject.Count > 0))
            {
                foreach (var key in oldObject.Keys.Union(newObject.Keys).OrderBy(k => k, StringComparer.Ordinal))
                {
                    var oldExists = oldObject.TryGetValue(key, out var oldValue);
                    var newExists = newObject.TryGetValue(key, out var newValue);
                    Compare(path + "/" + Escape(key), oldValue, oldExists, newValue, newExists);
                }
                return;
            }
            if (hasBefore && hasAfter && before is IList oldList && after is IList newList &&
                (oldList.Count > 0 || newList.Count > 0))
            {
                for (var i = 0; i < Math.Max(oldList.Count, newList.Count); i++)
                    Compare(path + "/" + i, i < oldList.Count ? oldList[i] : null, i < oldList.Count,
                        i < newList.Count ? newList[i] : null, i < newList.Count);
                return;
            }
            var oldText = hasBefore ? Serialize(before) : "Absent";
            var newText = hasAfter ? Serialize(after) : "Removed";
            Rows.Add(new Row { Path = path, Before = oldText, After = newText,
                Kind = !hasAfter ? Change.Removed : !hasBefore ? Change.Added :
                    oldText == newText ? Change.Unchanged : Change.Changed });
        }
    }
}
