using Beamable.Common.Content;
using cli.Portal;
using cli.Services;
using cli.Services.PortalExtension;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace tests.PortalExtensionTests;

public class RemotePortalConfigServiceTests
{
	private string _extensionDir;

	[SetUp]
	public void SetUp()
	{
		_extensionDir = Path.Combine(Path.GetTempPath(), "pe-mountsite-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_extensionDir);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_extensionDir))
			Directory.Delete(_extensionDir, true);
	}

	private void WriteSource(string relativePath, string contents)
	{
		var full = Path.Combine(_extensionDir, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllText(full, contents);
	}

	private static ExtensionBuildMetaData MakeMetadata(
		string name, string page, string navGroup, string navLabel, params string[] extensionSites) => new()
	{
		Name = name,
		ExtensionSites = extensionSites.ToList(),
		Properties = new PortalExtensionPackageProperties
		{
			IsPortalExtension = true,
			Mounts = new List<PortalExtensionMountProperties>
			{
				new()
				{
					Page = page,
					NavGroup = navGroup == null ? null : new OptionalString { HasValue = true, Value = navGroup },
					NavLabel = navLabel == null ? null : new OptionalString { HasValue = true, Value = navLabel },
				}
			}
		}
	};

	// --- parse / scan ------------------------------------------------------------------------------

	[Test]
	public void ParseExtensionSiteSelectors_ExtractsAllNames_InSourceOrder()
	{
		var text = @"
			<BeamExtensionSite selector=""top"" />
			<BeamExtensionSite selector='bottom' />
			<BeamExtensionSite selector=""B-top"" />
		";

		var selectors = RemotePortalConfigService.ParseExtensionSiteSelectors(text).ToList();

		Assert.That(selectors, Is.EqualTo(new[] { "top", "bottom", "B-top" }));
	}

	[Test]
	public void ScanExtensionSiteSelectors_ReturnsAllNames_AndExcludesNodeModules()
	{
		WriteSource(Path.Combine("src", "App.tsx"),
			@"<BeamExtensionSite selector=""top"" /><BeamExtensionSite selector=""B-top"" />");
		// Must be ignored — lives under node_modules.
		WriteSource(Path.Combine("node_modules", "pkg", "index.tsx"), @"<BeamExtensionSite selector=""sidebar"" />");

		var selectors = RemotePortalConfigService.ScanExtensionSiteSelectors(_extensionDir);

		Assert.That(selectors, Is.EqualTo(new[] { "top", "B-top" }));
	}

	[Test]
	public void ParseExtensionSiteSelectors_HandlesExtraPropsAnyOrder_AndExpressionForm()
	{
		var text = @"
			<BeamExtensionSite selector=""player-tabs"" mountKind=""tabs-route"" />
			<BeamExtensionSite mountKind=""x"" selector=""after-other-props"" />
			<BeamExtensionSite onReady={() => init()} selector=""after-arrow-fn"" />
			<BeamExtensionSite selector={'expr-single'} />
			<BeamExtensionSite
				selector=""multiline""
				mountKind=""y""
			/>
		";

		var selectors = RemotePortalConfigService.ParseExtensionSiteSelectors(text).ToList();

		Assert.That(selectors, Is.EqualTo(new[]
		{
			"player-tabs", "after-other-props", "after-arrow-fn", "expr-single", "multiline"
		}));
	}

	[Test]
	public void ParseExtensionSiteSelectors_DoesNotMatchSimilarlyNamedProps()
	{
		var text = @"<BeamExtensionSite data-selector=""nope"" selector=""yes"" />";

		var selectors = RemotePortalConfigService.ParseExtensionSiteSelectors(text).ToList();

		Assert.That(selectors, Is.EqualTo(new[] { "yes" }));
	}

	[Test]
	public void DeserializeMetadata_ToleratesNullOptionalFields()
	{
		// Regression: a mount with null nav-order fields used to abort the whole parse via the
		// OptionalConverter; the JObject-based reader must extract ExtensionSites + page regardless.
		var json = @"{
			""Name"": ""players-detail"",
			""ToolkitVersion"": ""0.0.1"",
			""Properties"": {
				""mounts"": [
					{ ""page"": ""players/list/:playerId/*"", ""selector"": ""#extension-page"",
					  ""navGroup"": null, ""navLabel"": null, ""navGroupOrder"": null, ""navLabelOrder"": null }
				]
			},
			""ExtensionSites"": [ ""player-tabs"" ]
		}";

		var md = RemotePortalConfigService.DeserializeMetadata(json);

		Assert.That(md, Is.Not.Null);
		Assert.That(md.ExtensionSites, Is.EqualTo(new[] { "player-tabs" }));
		Assert.That(md.Properties.Mounts, Has.Count.EqualTo(1));
		Assert.That(md.Properties.Mounts[0].Page, Is.EqualTo("players/list/:playerId/*"));

		// And it builds the expected mount site end-to-end.
		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(md);
		Assert.That(sites, Has.Count.EqualTo(1));
		Assert.That(sites[0].path, Is.EqualTo("players/list/:playerId/*"));
		Assert.That(sites[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "#player-tabs" }));
	}

	[Test]
	public void DeserializeMetadata_MissingExtensionSites_IsNullNotError()
	{
		var json = @"{ ""Name"": ""old-ext"", ""Properties"": { ""mounts"": [ { ""page"": ""players"" } ] } }";

		var md = RemotePortalConfigService.DeserializeMetadata(json);

		Assert.That(md, Is.Not.Null);
		Assert.That(md.ExtensionSites, Is.Null);
		Assert.That(RemotePortalConfigService.BuildMountSitesFromMetadata(md), Is.Empty);
	}

	[Test]
	public void ScanExtensionSiteSelectors_DeDuplicates()
	{
		WriteSource(Path.Combine("src", "App.tsx"),
			@"<BeamExtensionSite selector=""top"" /><BeamExtensionSite selector=""top"" />");

		var selectors = RemotePortalConfigService.ScanExtensionSiteSelectors(_extensionDir);

		Assert.That(selectors, Is.EqualTo(new[] { "top" }));
	}

	[Test]
	public void ScanExtensionSiteSelectors_ReturnsEmpty_WhenNoUsages()
	{
		WriteSource(Path.Combine("src", "App.tsx"), "export const App = () => null;");

		var selectors = RemotePortalConfigService.ScanExtensionSiteSelectors(_extensionDir);

		Assert.That(selectors, Is.Empty);
	}

	// --- build mount sites from metadata -----------------------------------------------------------

	[Test]
	public void BuildMountSitesFromMetadata_PageExtension_PathIsRoute_ComponentSelectors_WithNav()
	{
		var metadata = MakeMetadata("A", "ARoute", "AGroup", "ALabel", "top", "bottom");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Has.Count.EqualTo(1));
		var site = sites[0];
		Assert.That(site.path, Is.EqualTo("ARoute"));
		Assert.That(site.selectors.Select(s => s.selector), Is.EqualTo(new[] { "#top", "#bottom" }));
		Assert.That(site.selectors.All(s => s.type == "component"), Is.True);
		Assert.That(site.navContext, Is.EqualTo(new[] { "AGroup", "ALabel" }));
	}

	[Test]
	public void BuildMountSitesFromMetadata_ComponentExtension_PathIsHostPage()
	{
		// B is mounted at the players page; its slot lives there.
		var metadata = MakeMetadata("B", "players", null, null, "B-top");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Has.Count.EqualTo(1));
		Assert.That(sites[0].path, Is.EqualTo("players"));
		Assert.That(sites[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "#B-top" }));
		Assert.That(sites[0].navContext, Is.Empty);
	}

	[Test]
	public void BuildMountSitesFromMetadata_NormalizesLeadingAndTrailingSlashes()
	{
		var metadata = MakeMetadata("A", "/ARoute/", null, null, "top");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites[0].path, Is.EqualTo("ARoute"));
	}

	[Test]
	public void BuildMountSitesFromMetadata_OneSitePerMount()
	{
		var metadata = MakeMetadata("A", "ARoute", null, null, "top");
		metadata.Properties.Mounts.Add(new PortalExtensionMountProperties { Page = "players" });

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites.Select(s => s.path), Is.EquivalentTo(new[] { "ARoute", "players" }));
		Assert.That(sites.All(s => s.selectors.Select(x => x.selector).SequenceEqual(new[] { "#top" })), Is.True);
	}

	[Test]
	public void BuildMountSitesFromMetadata_PreservesAlreadyHashedSelector()
	{
		var metadata = MakeMetadata("A", "ARoute", null, null, "#top");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "#top" }));
	}

	[Test]
	public void BuildMountSitesFromMetadata_Empty_WhenNoExtensionSites()
	{
		var metadata = MakeMetadata("A", "ARoute", "AGroup", "ALabel");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Is.Empty);
	}

	[Test]
	public void BuildMountSitesFromMetadata_Empty_WhenNoName()
	{
		var metadata = MakeMetadata(null, "ARoute", null, null, "top");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Is.Empty);
	}

	[Test]
	public void BuildMountSitesFromMetadata_Empty_WhenNoResolvableMountPage()
	{
		var metadata = MakeMetadata("A", "   ", null, null, "top");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Is.Empty);
	}

	// --- merge -------------------------------------------------------------------------------------

	private static RemotePortalConfiguration.MountSiteConfig Site(string path, params string[] selectors) => new()
	{
		path = path,
		selectors = selectors.Select(s => new RemotePortalConfiguration.MountSiteSelector
		{
			selector = s,
			type = "component"
		}).ToList()
	};

	[Test]
	public void MergeMountSites_AddsSiteForNewPath()
	{
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("players", "#some-slot") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("ARoute", "#top") });

		Assert.That(target.Select(s => s.path), Is.EqualTo(new[] { "players", "ARoute" }));
	}

	[Test]
	public void MergeMountSites_UnionsSelectorsIntoExistingPath()
	{
		// B's #B-top folds into A's existing ARoute site alongside A's own slots.
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("ARoute", "#top", "#bottom") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("ARoute", "#B-top") });

		Assert.That(target, Has.Count.EqualTo(1));
		Assert.That(target[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "#top", "#bottom", "#B-top" }));
	}

	[Test]
	public void MergeMountSites_DeDuplicatesIdenticalSelectorAtSamePath()
	{
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("players", "#top") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("players", "#top", "#new") });

		Assert.That(target, Has.Count.EqualTo(1));
		Assert.That(target[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "#top", "#new" }));
	}

	[Test]
	public void MergeMountSites_FillsNavContextFromAddition_WhenTargetHasNone()
	{
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("ARoute", "#top") };
		var addition = Site("ARoute", "#B-top");
		addition.navContext = new List<string> { "AGroup", "ALabel" };

		RemotePortalConfigService.MergeMountSites(target, new[] { addition });

		Assert.That(target[0].navContext, Is.EqualTo(new[] { "AGroup", "ALabel" }));
	}
}
