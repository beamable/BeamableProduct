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
    }
}
