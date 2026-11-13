using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace tests.Unity;

public class FederationGeneratedModelCopyTests
{
	[TestCase("CampaignOffer")]
	[TestCase("CampaignOfferAmount")]
	[TestCase("CampaignOfferGrantContext")]
	[TestCase("CampaignOfferGrantResponse")]
	[TestCase("CampaignOfferItem")]
	[TestCase("CampaignOfferReason")]
	[TestCase("CampaignOfferRedeemResponse")]
	[TestCase("CampaignOfferState")]
	[TestCase("CampaignOfferText")]
	[TestCase("CampaignOffersResponse")]
	[TestCase("MessageRailRegistrationResponse")]
	public void UnityModelAndGeneratedDependenciesMatchCanonicalOutput(string rootType)
	{
		var repository = FindRepository();
		var canonical = ReadDeclarations(Path.Combine(repository, "cli/beamable.common/Runtime/OpenApi/Models.gs.cs"));
		var unity = ReadDeclarations(Path.Combine(repository, "client/Packages/com.beamable/Common/Runtime/OpenApi/Models.gs.cs"));
		var pending = new Queue<string>();
		var visited = new HashSet<string>();
		pending.Enqueue(rootType);
		while (pending.Count > 0)
		{
			var name = pending.Dequeue();
			if (!visited.Add(name)) continue;
			Assert.That(canonical.ContainsKey(name), Is.True, $"Canonical model missing: {name}");
			Assert.That(unity.TryGetValue(name, out var copied), Is.True, $"Unity model missing: {name}");
			Assert.That(copied, Is.EqualTo(canonical[name]), $"Refresh {name} from canonical generated output.");
			// Follow generated type references, excluding wire keys and other string literals.
			var code = Regex.Replace(canonical[name], "\"(?:\\\\.|[^\"\\\\])*\"", "");
			foreach (Match identifier in Regex.Matches(code, @"\b\w+\b"))
				if (canonical.ContainsKey(identifier.Value)) pending.Enqueue(identifier.Value);
		}
	}

	private static Dictionary<string, string> ReadDeclarations(string path)
	{
		// Models.gs.cs emits namespace-level declarations at four spaces. Nested members
		// are indented further, so the closing brace anchors each complete generated type.
		const string pattern = @"(?:^    \[[^\n]*\]\n)*^    public (?:partial )?(?:class|enum) (\w+)[^\n]*\n    \{.*?^    \}";
		return Regex.Matches(File.ReadAllText(path).ReplaceLineEndings("\n"), pattern,
				RegexOptions.Multiline | RegexOptions.Singleline)
			.Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Value);
	}

	private static string FindRepository()
	{
		for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
		     directory != null; directory = directory.Parent)
			if (File.Exists(Path.Combine(directory.FullName, "cli/beamable.common/beamable.common.csproj")))
				return directory.FullName;
		throw new DirectoryNotFoundException("The generated-model copy test requires the repository checkout.");
	}
}
