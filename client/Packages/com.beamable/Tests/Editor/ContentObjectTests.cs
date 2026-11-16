using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class ContentObjectTests
	{
		[Test]
		public void First_checksum_observation_does_not_save_but_a_later_change_does()
		{
			var content = ScriptableObject.CreateInstance<TestContent>();
			try
			{
				content.CancelPendingEditorChangeNotification();
				typeof(ContentObject).GetField("_lastChecksum", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
					.SetValue(content, string.Empty);
				content.CheckForNonDetectedChanges();
				Assert.That(content.HasPendingEditorChangeNotification, Is.False);
				content.number++;
				content.CheckForNonDetectedChanges();
				Assert.That(content.HasPendingEditorChangeNotification, Is.True);
			}
			finally { content.CancelPendingEditorChangeNotification(); Object.DestroyImmediate(content); }
		}

		[Test]
		public void Validate_WellConfigured_Test()
		{
			var sampleValidationCtx = new ValidationContext();

			var testContent = ScriptableObject.CreateInstance<TestContent>();
			testContent.number = 0;
			Assert.DoesNotThrow(() => testContent.Validate(sampleValidationCtx));

			testContent = ScriptableObject.CreateInstance<TestContent>();
			testContent.number = 20;
			Assert.DoesNotThrow(() => testContent.Validate(sampleValidationCtx));

			testContent = ScriptableObject.CreateInstance<TestContent>();
			testContent.number = -32;
			Assert.Throws<AggregateContentValidationException>(() => testContent.Validate(sampleValidationCtx));
		}

		[Test]
		public void Validate_PoorlyConfigured_Test()
		{
			var sampleValidationCtx = new ValidationContext();

			var content = ScriptableObject.CreateInstance<PoorlyConfiguredContent>();
			content.s = "boo!";
			content.number = -32;
			Assert.Throws<AggregateContentValidationException>(() => content.Validate(sampleValidationCtx));
		}
	}

	public class TestContent : ContentObject
	{
		[MustBePositive(AllowZero = true)]
		public int number;

		public TestContent(int number)
		{
			this.number = number;
		}
	}

	public class PoorlyConfiguredContent : ContentObject
	{
		[MustBePositive]
		public string s;

		[MustBePositive]
		public int number;

		public PoorlyConfiguredContent(string s, int number)
		{
			this.s = s;
			this.number = number;
		}
	}
}
