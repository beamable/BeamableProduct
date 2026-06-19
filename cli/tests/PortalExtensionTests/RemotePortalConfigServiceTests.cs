using Beamable.Common.Content;
using cli.Portal;
using cli.Services;
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

	private static PortalExtensionDef MakeDef(string absPath, string page, string navGroup, string navLabel) => new()
	{
		Name = "MyExt",
		AbsolutePath = absPath,
		Properties = new PortalExtensionPackageProperties
		{
			IsPortalExtension = true,
			Mount = new PortalExtensionMountProperties
			{
				Page = page,
				NavGroup = navGroup == null ? null : new OptionalString { HasValue = true, Value = navGroup },
				NavLabel = navLabel == null ? null : new OptionalString { HasValue = true, Value = navLabel },
			}
		}
	};

	[Test]
	public void ParseExtensionSiteSelectors_ExtractsTopAndBottom_IgnoresOthers()
	{
		var text = @"
			<BeamExtensionSite selector=""top"" />
			<BeamExtensionSite selector='bottom' />
			<BeamExtensionSite selector=""sidebar"" />
		";

		var selectors = RemotePortalConfigService.ParseExtensionSiteSelectors(text).ToList();

		Assert.That(selectors, Is.EquivalentTo(new[] { "top", "bottom" }));
	}

	[Test]
	public void ScanExtensionSiteSelectors_ReturnsTopThenBottom_AndExcludesNodeModules()
	{
		WriteSource(Path.Combine("src", "App.tsx"), @"<BeamExtensionSite selector=""bottom"" />");
		WriteSource(Path.Combine("src", "Header.tsx"), @"<BeamExtensionSite selector=""top"" />");
		// Must be ignored — lives under node_modules.
		WriteSource(Path.Combine("node_modules", "pkg", "index.tsx"), @"<BeamExtensionSite selector=""sidebar"" />");

		var selectors = RemotePortalConfigService.ScanExtensionSiteSelectors(_extensionDir);

		Assert.That(selectors.Select(s => s.selector), Is.EqualTo(new[] { "#top", "#bottom" }));
		Assert.That(selectors.All(s => s.type == RemotePortalConfigService.ExtensionMountType), Is.True);
	}

	[Test]
	public void ScanExtensionSiteSelectors_ReturnsEmpty_WhenNoUsages()
	{
		WriteSource(Path.Combine("src", "App.tsx"), "export const App = () => null;");

		var selectors = RemotePortalConfigService.ScanExtensionSiteSelectors(_extensionDir);

		Assert.That(selectors, Is.Empty);
	}

	[Test]
	public void BuildLocalExtensionMountSites_PathIsExtensionName_WithExtensionTypeSelectors()
	{
		WriteSource(Path.Combine("src", "App.tsx"),
			@"<BeamExtensionSite selector=""top"" /><BeamExtensionSite selector=""bottom"" />");

		// page is unrelated to the produced path — the site is keyed by the extension's NAME,
		// which is how a child extension references it (its own mount.page = this name).
		var def = MakeDef(_extensionDir, "players/insights", "Engage", "Player Insights");

		var sites = RemotePortalConfigService.BuildLocalExtensionMountSites(new[] { def });

		Assert.That(sites, Has.Count.EqualTo(1));
		var site = sites[0];
		Assert.That(site.path, Is.EqualTo("MyExt"));
		Assert.That(site.selectors.Select(s => s.selector), Is.EqualTo(new[] { "#top", "#bottom" }));
		Assert.That(site.selectors.All(s => s.type == RemotePortalConfigService.ExtensionMountType), Is.True);
		Assert.That(site.navContext, Is.EqualTo(new[] { "Engage", "Player Insights" }));
	}

	[Test]
	public void BuildLocalExtensionMountSites_NavContextEmpty_WhenNavUnset()
	{
		WriteSource(Path.Combine("src", "App.tsx"), @"<BeamExtensionSite selector=""top"" />");

		var def = MakeDef(_extensionDir, null, null, null);

		var sites = RemotePortalConfigService.BuildLocalExtensionMountSites(new[] { def });

		Assert.That(sites, Has.Count.EqualTo(1));
		Assert.That(sites[0].path, Is.EqualTo("MyExt"));
		Assert.That(sites[0].navContext, Is.Empty);
	}

	[Test]
	public void BuildLocalExtensionMountSites_SkipsExtension_WithNoExtensionSites()
	{
		WriteSource(Path.Combine("src", "App.tsx"), "export const App = () => null;");

		var def = MakeDef(_extensionDir, "players/insights", "Engage", "Player Insights");

		var sites = RemotePortalConfigService.BuildLocalExtensionMountSites(new[] { def });

		Assert.That(sites, Is.Empty);
	}

	[Test]
	public void BuildLocalExtensionMountSites_SkipsExtension_WithNoName()
	{
		WriteSource(Path.Combine("src", "App.tsx"), @"<BeamExtensionSite selector=""top"" />");

		var def = MakeDef(_extensionDir, "players/insights", "Engage", "Player Insights");
		def.Name = null;

		var sites = RemotePortalConfigService.BuildLocalExtensionMountSites(new[] { def });

		Assert.That(sites, Is.Empty);
	}

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
		// A page extension's unique route — no existing site owns it, so it's added verbatim.
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("players", "#some-slot") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("players/insights", "#top") });

		Assert.That(target.Select(s => s.path), Is.EqualTo(new[] { "players", "players/insights" }));
	}

	[Test]
	public void MergeMountSites_MergesSelectorsIntoExistingPath_WithoutDuplicating()
	{
		// A component extension on the existing "players" page: its #top slot is added to that
		// page's site rather than creating a second "players" entry; #some-slot is preserved.
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("players", "#some-slot") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("players", "#top", "#some-slot") });

		Assert.That(target, Has.Count.EqualTo(1));
		Assert.That(target[0].path, Is.EqualTo("players"));
		Assert.That(target[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "#some-slot", "#top" }));
	}
}
