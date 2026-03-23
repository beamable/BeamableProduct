using cli.Services.PortalExtension;
using NUnit.Framework;
using spkl.Diffs;

namespace tests.PortalExtensionTests;

public class PortalExtensionDiffTests
{
	/// <summary>
	/// Round-trips the diff: produces B from A using GetDiffInstructions + GetResult
	/// and asserts the output matches B exactly.
	/// </summary>
	static string[] RoundTrip(string[] a, string[] b)
	{
		var diff         = new MyersDiff<string>(a, b);
		var instructions = PortalExtensionDiff.GetDiffInstructions(a, b);
		return PortalExtensionDiff.GetResult(diff, a, instructions.LinesToAdd ?? new string[]{});
	}

	// ── HasChanges ────────────────────────────────────────────────────────────

	[TestCase(new string[] { "line1", "line2", "line3" }, new string[] { "line1", "line2", "line3" }, false, TestName = "IdenticalArrays_HasNoChanges")]
	[TestCase(new string[] { "line1", "old line", "line3" }, new string[] { "line1", "new line", "line3" }, true, TestName = "SingleLineReplaced_HasChanges")]
	public void GetDiffInstructions_HasChanges_IsCorrect(string[] a, string[] b, bool expected)
	{
		var instructions = PortalExtensionDiff.GetDiffInstructions(a, b);

		Assert.That(instructions.HasChanges, Is.EqualTo(expected));
	}

	// ── Round-trip ────────────────────────────────────────────────────────────

	[TestCase(new string[] { "line1", "line2", "line3" }, new string[] { "line1", "line2", "line3" }, TestName = "IdenticalArrays")]
	[TestCase(new string[] { "line1", "old line", "line3" }, new string[] { "line1", "new line", "line3" }, TestName = "SingleLineReplaced")]
	[TestCase(new string[] { "line1", "line2" }, new string[] { "line1", "line2", "line3", "line4" }, TestName = "LinesAddedAtEnd")]
	[TestCase(new string[] { "line1", "line2", "line3", "line4" }, new string[] { "line1", "line4" }, TestName = "LinesRemoved")]
	[TestCase(new string[] { "line3", "line4" }, new string[] { "line1", "line2", "line3", "line4" }, TestName = "LinesAddedAtBeginning")]
	[TestCase(new string[] { }, new string[] { "line1", "line2", "line3" }, TestName = "EmptyA")]
	[TestCase(new string[] { "line1", "line2", "line3" }, new string[] { }, TestName = "EmptyB")]
	[TestCase(new string[] { "alpha", "beta", "gamma" }, new string[] { "one", "two", "three" }, TestName = "CompleteReplacement")]
	[TestCase(new string[] { "keep1", "remove1", "keep2", "remove2", "keep3" }, new string[] { "keep1", "added1", "keep2", "added2", "keep3" }, TestName = "MultipleDisjointChanges")]
	public void RoundTrip_ProducesB(string[] a, string[] b)
	{
		Assert.That(RoundTrip(a, b), Is.EqualTo(b));
	}

	// ── Instructions content ──────────────────────────────────────────────────

	[Test]
	public void LinesToAdd_ContainsOnlyAddedLines()
	{
		var a = new[] { "line1", "old", "line3" };
		var b = new[] { "line1", "new", "line3" };

		var instructions = PortalExtensionDiff.GetDiffInstructions(a, b);

		Assert.That(instructions.LinesToAdd, Is.EqualTo(new[] { "new" }));
	}

	[Test]
	public void Instructions_CountMatchesNumberOfEditOperations()
	{
		var a = new[] { "line1", "old1", "line3", "old2", "line5" };
		var b = new[] { "line1", "new1", "line3", "new2", "line5" };

		var instructions = PortalExtensionDiff.GetDiffInstructions(a, b);

		Assert.That(instructions.Instructions, Has.Length.EqualTo(2));
	}
}
