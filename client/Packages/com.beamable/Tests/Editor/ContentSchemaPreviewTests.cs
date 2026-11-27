using System;
using System.Linq;
using Beamable.Editor.UI.ContentWindow;
using NUnit.Framework;

namespace Beamable.Editor.Tests
{
    public class ContentSchemaPreviewTests
    {
        [Test]
        public void Nested_arrays_nulls_and_escaped_keys_have_distinct_review_paths()
        {
            var comparison = new ContentSchemaPreview(
                "{\"properties\":{\"nested\":{\"data\":[{\"extra\":37,\"keep\":null}]},\"a/b~c\":{\"data\":null}}}",
                "{\"nested\":{\"data\":[{\"keep\":null,\"new\":0}]},\"a/b~c\":{\"data\":null}}");
            Assert.That(comparison.Rows.Single(r => r.Path == "/nested/0/extra").Kind, Is.EqualTo(ContentSchemaPreview.Change.Removed));
            Assert.That(comparison.Rows.Single(r => r.Path == "/nested/0/new").Kind, Is.EqualTo(ContentSchemaPreview.Change.Added));
            Assert.That(comparison.Rows.Single(r => r.Path == "/nested/0/keep").Kind, Is.EqualTo(ContentSchemaPreview.Change.Unchanged));
            Assert.That(comparison.Rows.Single(r => r.Path == "/a~1b~0c").Before, Is.EqualTo("null"));
        }

        [Test]
        public void Changed_values_are_not_misrepresented_as_default_additions()
        {
            var comparison = new ContentSchemaPreview("{\"properties\":{\"value\":{\"data\":\"20\"}}}",
                "{\"value\":{\"data\":20}}");
            Assert.That(comparison.Rows.Single().Kind, Is.EqualTo(ContentSchemaPreview.Change.Changed));
        }

        [TestCase("{}", "{}")]
        [TestCase("{\"properties\":null}", "{}")]
        [TestCase("{\"properties\":{}}", "[]")]
        public void Invalid_property_envelopes_cannot_be_reviewed_as_empty(string original, string proposed)
        {
            Assert.Throws<InvalidOperationException>(() => new ContentSchemaPreview(original, proposed));
        }

        [TestCase("18446744073709551615", "18446744073709551614")]
        [TestCase("0.123456789012345678901234567891", "0.123456789012345678901234567892")]
        [TestCase("1e999", "2e999")]
        public void Numeric_tokens_are_exact_in_rows_and_both_json_tabs(string before, string after)
        {
            var comparison = new ContentSchemaPreview("{\"properties\":{\"value\":{\"data\":" + before + "}}}",
                "{\"value\":{\"data\":" + after + "}}");
            var row = comparison.Rows.Single();
            Assert.That(row.Kind, Is.EqualTo(ContentSchemaPreview.Change.Changed));
            Assert.That(row.Before, Is.EqualTo(before));
            Assert.That(row.After, Is.EqualTo(after));
            Assert.That(comparison.CurrentJson, Does.Contain(":" + before));
            Assert.That(comparison.ProposedJson, Does.Contain(":" + after));
        }

        [Test]
        public void Removed_unknown_objects_retain_exact_numbers_and_string_types()
        {
            var comparison = new ContentSchemaPreview(
                "{\"properties\":{\"unknown\":{\"data\":{\"\":[18446744073709551615,-9223372036854775808],\"item\":\"18446744073709551615\",\"a/b~c\":true}}}}", "{}");
            var row = comparison.Rows.Single();
            Assert.That(row.Kind, Is.EqualTo(ContentSchemaPreview.Change.Removed));
            Assert.That(row.Before, Is.EqualTo("{\"\":[18446744073709551615,-9223372036854775808],\"item\":\"18446744073709551615\",\"a/b~c\":true}"));
            Assert.That(comparison.CurrentJson, Does.Contain("18446744073709551615"));
        }

        [TestCase("\"  \"")]
        [TestCase("\"\\r\\n\"")]
        [TestCase("\"\\\"123\\\"\"")]
        public void String_contents_are_not_interpreted_as_numbers_or_formatting(string value)
        {
            var properties = "{\"value\":{\"data\":" + value + "}}";
            var comparison = new ContentSchemaPreview("{\"properties\":" + properties + "}", properties);
            Assert.That(comparison.Rows.Single().Before, Is.EqualTo(value));
            Assert.That(comparison.Rows.Single().Kind, Is.EqualTo(ContentSchemaPreview.Change.Unchanged));
        }
    }
}
