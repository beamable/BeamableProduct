using Beamable.Common.Content;
using cli.Portal;
using cli.Services;
using cli.Services.PortalExtension;
using Newtonsoft.Json.Linq;
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

	private static PortalExtensionSiteDeclaration Decl(string id, string presentation = "stack", string description = null) =>
		new() { Id = id, Presentation = presentation, Description = description };

	private static PortalExtensionPackageProperties Package(
		List<PortalExtensionSiteDeclaration> sites, params string[][] mountSiteIds) => new()
	{
		IsPortalExtension = true,
		Sites = sites,
		Mounts = mountSiteIds.Select(ids => new PortalExtensionMountProperties { SiteIds = ids.ToList() }).ToList()
	};

	// --- declarations reach metadata.json ----------------------------------------------------------

	private PortalExtensionObserver WriteMetadata(PortalExtensionPackageProperties properties)
	{
		var observer = new PortalExtensionObserver
		{
			ExtensionMetaData = new PortalExtensionDef { Name = "campaigns", AbsolutePath = _extensionDir, Properties = properties }
		};
		observer.CreateMetaDataFile();
		return observer;
	}

	[Test]
	public void CreateMetaDataFile_WritesDeclarations_AndExtensionSitesFromSitesIds()
	{
		var properties = Package(new List<PortalExtensionSiteDeclaration>
		{
			Decl("campaign-offer", "stack", "An offer a campaign lane sends with its message."),
			Decl("message-rail", "stack"),
		}, new[] { "extension-page" });
		properties.Mounts[0].Page = "campaigns";
		properties.Sites[0].Extra = new Dictionary<string, JToken> { ["static"] = new JObject { ["type"] = "object" } };

		var root = JObject.Parse(File.ReadAllText(WriteMetadata(properties).MetadataPath));

		Assert.That(root["ExtensionSites"].Select(t => (string)t), Is.EqualTo(new[] { "campaign-offer", "message-rail" }));
		var sites = (JArray)root["Properties"]["sites"];
		Assert.That((string)sites[0]["id"], Is.EqualTo("campaign-offer"));
		Assert.That((string)sites[0]["presentation"], Is.EqualTo("stack"));
		Assert.That((string)sites[0]["description"], Does.StartWith("An offer"));
		Assert.That(sites[0]["static"]?["type"]?.ToString(), Is.EqualTo("object"), "Step 2 fields pass through");
		Assert.That(((JObject)sites[1]).ContainsKey("description"), Is.False, "an absent description is not written as null");

		var mount = (JObject)root["Properties"]["mounts"][0];
		Assert.That(mount["siteIds"].Select(t => (string)t), Is.EqualTo(new[] { "extension-page" }));
		Assert.That(mount.ContainsKey("id"), Is.False, "an absent mount id is not written");
		Assert.That(mount.ContainsKey("selector"), Is.False);
	}

	[Test]
	public void CreateMetaDataFile_StackMount_WritesNoPageAndNoNulls()
	{
		var properties = Package(null, new[] { "campaign-offer" });
		properties.Mounts[0].Id = new OptionalString { HasValue = true, Value = "reward" };

		var root = JObject.Parse(File.ReadAllText(WriteMetadata(properties).MetadataPath));

		Assert.That(root["ExtensionSites"], Is.Empty, "no declarations, no ExtensionSites");
		Assert.That(((JObject)root["Properties"]).ContainsKey("sites"), Is.False);
		var mount = (JObject)root["Properties"]["mounts"][0];
		Assert.That((string)mount["id"], Is.EqualTo("reward"));
		Assert.That(mount.ContainsKey("page"), Is.False, "a stack mount has no page");
	}

	[Test]
	public void CreateMetaDataFile_FillsExtensionSitesFromDeclarations_NotFromSource()
	{
		File.WriteAllText(Path.Combine(_extensionDir, "App.tsx"), @"<BeamExtensionSite selector=""scanned"" />");

		var root = JObject.Parse(File.ReadAllText(WriteMetadata(Package(new List<PortalExtensionSiteDeclaration> { Decl("declared") })).MetadataPath));

		Assert.That(root["ExtensionSites"].Select(t => (string)t), Is.EqualTo(new[] { "declared" }));
	}

	// --- declaration checks ------------------------------------------------------------------------

	[Test]
	public void DeclarationChecks_ValidDeclarations_HaveNoErrors_AndNeedNoDescription()
	{
		var properties = Package(new List<PortalExtensionSiteDeclaration>
		{
			Decl("player-profile-tabs", "route-tabs"), Decl("a", "tabs"), Decl("b", "stack")
		}, new[] { "extension-page" }, new[] { "campaign-offer", "campaign-offer-v2" });

		Assert.That(PortalExtensionSiteDeclarationChecks.GetErrors(properties), Is.Empty);
		Assert.That(PortalExtensionSiteDeclarationChecks.GetErrorMessage("x", properties), Is.Null);
	}

	[TestCase(null)]
	[TestCase("grid")]
	[TestCase("host")]
	[TestCase("Stack")]
	public void DeclarationChecks_UnknownPresentation_IsAnError(string presentation)
	{
		var errors = PortalExtensionSiteDeclarationChecks.GetErrors(
			Package(new List<PortalExtensionSiteDeclaration> { Decl("campaign-offer", presentation) }));

		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors[0], Does.Contain("campaign-offer").And.Contain("presentation"));
	}

	[Test]
	public void DeclarationChecks_TwoSitesWithOneId_IsAnError()
	{
		var errors = PortalExtensionSiteDeclarationChecks.GetErrors(Package(new List<PortalExtensionSiteDeclaration>
		{
			Decl("campaign-offer"), Decl("campaign-offer", "tabs")
		}));

		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors[0], Does.Contain("campaign-offer").And.Contain("more than once"));
	}

	[Test]
	public void DeclarationChecks_SiteWithoutId_IsAnError()
	{
		var errors = PortalExtensionSiteDeclarationChecks.GetErrors(
			Package(new List<PortalExtensionSiteDeclaration> { Decl(null) }));

		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors[0], Does.Contain("sites[0]"));
	}

	[TestCase("#extension-page")]
	[TestCase("campaign-offer@2")]
	[TestCase("@campaigns/offer")]
	public void DeclarationChecks_HashOrAtInSiteIds_IsAnError(string siteId)
	{
		var errors = PortalExtensionSiteDeclarationChecks.GetErrors(Package(null, new[] { "ok" }, new[] { siteId }));

		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors[0], Does.Contain("mounts[1]").And.Contain(siteId));
	}

	[Test]
	public void DeclarationChecks_HashInDeclaredId_IsAnError()
	{
		var errors = PortalExtensionSiteDeclarationChecks.GetErrors(
			Package(new List<PortalExtensionSiteDeclaration> { Decl("#offer") }));

		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors[0], Does.Contain("#offer"));
	}

	[Test]
	public void DeclarationChecks_ErrorMessage_NamesTheExtensionAndEveryError()
	{
		var message = PortalExtensionSiteDeclarationChecks.GetErrorMessage("campaigns", Package(
			new List<PortalExtensionSiteDeclaration> { Decl("x", "grid") }, new[] { "#y" }));

		Assert.That(message, Does.Contain("campaigns").And.Contain("grid").And.Contain("#y"));
	}

	// --- deserialize -------------------------------------------------------------------------------

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
					{ ""page"": ""players/list/:playerId/*"", ""siteIds"": [ ""extension-page"" ], ""id"": null,
					  ""navGroup"": null, ""navLabel"": null, ""navGroupOrder"": null, ""navLabelOrder"": null,
					  ""args"": null, ""filters"": null }
				]
			},
			""ExtensionSites"": [ ""player-profile-tabs"" ]
		}";

		var md = RemotePortalConfigService.DeserializeMetadata(json);

		Assert.That(md, Is.Not.Null);
		Assert.That(md.ExtensionSites, Is.EqualTo(new[] { "player-profile-tabs" }));
		Assert.That(md.Properties.Mounts, Has.Count.EqualTo(1));
		Assert.That(md.Properties.Mounts[0].Page, Is.EqualTo("players/list/:playerId/*"));
		Assert.That(md.Properties.Mounts[0].Id.HasValue, Is.False);
		Assert.That(md.Properties.Mounts[0].Args, Is.Empty);
		Assert.That(md.Properties.Mounts[0].Filters, Is.Empty);

		// And it builds the expected mount site end-to-end, with bare ids.
		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(md);
		Assert.That(sites, Has.Count.EqualTo(1));
		Assert.That(sites[0].path, Is.EqualTo("players/list/:playerId/*"));
		Assert.That(sites[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "player-profile-tabs" }));
	}

	[Test]
	public void DeserializeMetadata_ReadsSitesAndEachMountsIdSiteIdsArgsAndFilters()
	{
		var json = @"{
			""Name"": ""campaigns"",
			""Properties"": {
				""sites"": [
					{ ""id"": ""campaign-offer"", ""presentation"": ""stack"", ""description"": ""An offer."", ""static"": {} },
					{ ""id"": ""message-rail"", ""presentation"": ""stack"" }
				],
				""mounts"": [
					{ ""id"": ""reward"", ""siteIds"": [ ""campaign-offer"", ""campaign-offer-v2"" ],
					  ""args"": { ""federationId"": ""beamable_reward"", ""offerContractVersion"": 2 },
					  ""filters"": { ""account.role"": ""developer|admin"" } }
				]
			},
			""ExtensionSites"": [ ""campaign-offer"", ""message-rail"" ]
		}";

		var md = RemotePortalConfigService.DeserializeMetadata(json);

		Assert.That(md.Properties.Sites.Select(s => s.Id), Is.EqualTo(new[] { "campaign-offer", "message-rail" }));
		Assert.That(md.Properties.Sites[0].Presentation, Is.EqualTo("stack"));
		Assert.That(md.Properties.Sites[0].Description, Is.EqualTo("An offer."));
		Assert.That(md.Properties.Sites[1].Description, Is.Null);

		var mount = md.Properties.Mounts.Single();
		Assert.That(mount.Id.Value, Is.EqualTo("reward"));
		Assert.That(mount.SiteIds, Is.EqualTo(new[] { "campaign-offer", "campaign-offer-v2" }));
		Assert.That(mount.Page, Is.Null);
		Assert.That((int)mount.Args["offerContractVersion"], Is.EqualTo(2), "args keep their JSON types");
		Assert.That(mount.Filters["account.role"], Is.EqualTo("developer|admin"));
	}

	[Test]
	public void DeserializeMetadata_MissingExtensionSites_IsNullNotError()
	{
		var json = @"{ ""Name"": ""old-ext"", ""Properties"": { ""mounts"": [ { ""page"": ""players"" } ] } }";

		var md = RemotePortalConfigService.DeserializeMetadata(json);

		Assert.That(md, Is.Not.Null);
		Assert.That(md.ExtensionSites, Is.Null);
		Assert.That(md.Properties.Sites, Is.Null);
		Assert.That(md.Properties.Mounts[0].SiteIds, Is.Null);
		Assert.That(RemotePortalConfigService.BuildMountSitesFromMetadata(md), Is.Empty);
	}

	// --- build mount sites from metadata -----------------------------------------------------------

	[Test]
	public void BuildMountSitesFromMetadata_PageExtension_PathIsRoute_BareSiteIds_WithNav()
	{
		var metadata = MakeMetadata("A", "ARoute", "AGroup", "ALabel", "top", "bottom");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Has.Count.EqualTo(1));
		var site = sites[0];
		Assert.That(site.path, Is.EqualTo("ARoute"));
		Assert.That(site.selectors.Select(s => s.selector), Is.EqualTo(new[] { "top", "bottom" }));
		Assert.That(site.selectors.All(s => s.type == "component"), Is.True);
		// navContext carries the nav group then the label — the hub hierarchy lives in the page path.
		Assert.That(site.navContext, Is.EqualTo(new[] { "AGroup", "ALabel" }));
	}

	[Test]
	public void BuildMountSitesFromMetadata_ComponentExtension_PathIsHostPage()
	{
		// B is mounted at the players page; its site lives there.
		var metadata = MakeMetadata("B", "players", null, null, "B-top");

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites, Has.Count.EqualTo(1));
		Assert.That(sites[0].path, Is.EqualTo("players"));
		Assert.That(sites[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "B-top" }));
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
	public void BuildMountSitesFromMetadata_OneSitePerMountWithAPage()
	{
		var metadata = MakeMetadata("A", "ARoute", null, null, "top");
		metadata.Properties.Mounts.Add(new PortalExtensionMountProperties { Page = "players" });
		metadata.Properties.Mounts.Add(new PortalExtensionMountProperties { SiteIds = new List<string> { "campaign-offer" } });

		var sites = RemotePortalConfigService.BuildMountSitesFromMetadata(metadata);

		Assert.That(sites.Select(s => s.path), Is.EquivalentTo(new[] { "ARoute", "players" }));
		Assert.That(sites.All(s => s.selectors.Select(x => x.selector).SequenceEqual(new[] { "top" })), Is.True);
	}

	[Test]
	public void BuildMountSitesFromMetadata_Empty_WhenNoExtensionSites()
	{
		var metadata = MakeMetadata("A", "ARoute", null, "ALabel");

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

	private static RemotePortalConfiguration.MountSiteConfig Site(string path, params string[] siteIds) => new()
	{
		path = path,
		selectors = siteIds.Select(s => new RemotePortalConfiguration.MountSiteSelector
		{
			selector = s,
			type = "component"
		}).ToList()
	};

	[Test]
	public void MergeMountSites_AddsSiteForNewPath()
	{
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("players", "some-site") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("ARoute", "top") });

		Assert.That(target.Select(s => s.path), Is.EqualTo(new[] { "players", "ARoute" }));
	}

	[Test]
	public void MergeMountSites_UnionsSiteIdsIntoExistingPath()
	{
		// B's B-top folds into A's existing ARoute site alongside A's own sites.
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("ARoute", "top", "bottom") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("ARoute", "B-top") });

		Assert.That(target, Has.Count.EqualTo(1));
		Assert.That(target[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "top", "bottom", "B-top" }));
	}

	[Test]
	public void MergeMountSites_KeepsADuplicateIdAtSamePathOnce()
	{
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("players", "top") };

		RemotePortalConfigService.MergeMountSites(target, new[] { Site("players", "top", "new") });

		Assert.That(target, Has.Count.EqualTo(1));
		Assert.That(target[0].selectors.Select(s => s.selector), Is.EqualTo(new[] { "top", "new" }));
	}

	[Test]
	public void MergeMountSites_FillsNavContextFromAddition_WhenTargetHasNone()
	{
		var target = new List<RemotePortalConfiguration.MountSiteConfig> { Site("ARoute", "top") };
		var addition = Site("ARoute", "B-top");
		addition.navContext = new List<string> { "AGroup", "ALabel" };

		RemotePortalConfigService.MergeMountSites(target, new[] { addition });

		Assert.That(target[0].navContext, Is.EqualTo(new[] { "AGroup", "ALabel" }));
	}

	// --- site catalog + list-extension-options -----------------------------------------------------

	private static ExtensionBuildMetaData Declaring(string name, params PortalExtensionSiteDeclaration[] sites) => new()
	{
		Name = name,
		Properties = new PortalExtensionPackageProperties { Sites = sites.ToList() }
	};

	private static RemotePortalConfiguration CatalogConfig()
	{
		var config = new RemotePortalConfiguration
		{
			mountSites = new List<RemotePortalConfiguration.MountSiteConfig>
			{
				new()
				{
					path = "!hub/!pathMatch",
					selectors = new List<RemotePortalConfiguration.MountSiteSelector>
					{
						new() { selector = "extension-page", type = "page" }
					}
				},
				Site("players", "nav-left-low"),
			}
		};
		RemotePortalConfigService.AddPortalSites(config);
		return config;
	}

	[Test]
	public void SiteCatalog_ListsEachSiteOnce_WithItsRenderers()
	{
		var config = CatalogConfig();

		RemotePortalConfigService.AddDeclaredSites(config.sites, Declaring("campaigns",
			Decl("campaign-offer", "stack", "An offer."), Decl("message-rail")));
		RemotePortalConfigService.AddDeclaredSites(config.sites, Declaring("campaigns-v2",
			Decl("campaign-offer", "stack", "An offer.")));

		Assert.That(config.sites.Select(s => s.id),
			Is.EqualTo(new[] { "extension-page", "nav-left-low", "campaign-offer", "message-rail" }));
		Assert.That(config.sites[0].renderedBy, Is.EqualTo(new[] { RemotePortalConfigService.PortalSiteOwner }));
		var offer = config.sites.Single(s => s.id == "campaign-offer");
		Assert.That(offer.renderedBy, Is.EqualTo(new[] { "campaigns", "campaigns-v2" }));
		Assert.That(offer.presentation, Is.EqualTo("stack"));
		Assert.That(offer.description, Is.EqualTo("An offer."));
	}

	[Test]
	public void SiteCatalog_DivergentDeclarations_KeepTheFirst_AndBothRenderers()
	{
		var config = new RemotePortalConfiguration();

		RemotePortalConfigService.AddDeclaredSites(config.sites, Declaring("a", Decl("tabs-site", "tabs")));
		RemotePortalConfigService.AddDeclaredSites(config.sites, Declaring("b", Decl("tabs-site", "route-tabs")));

		var site = config.sites.Single();
		Assert.That(site.presentation, Is.EqualTo("tabs"));
		Assert.That(site.renderedBy, Is.EqualTo(new[] { "a", "b" }));
	}

	[Test]
	public void ListExtensionOptions_SplitsPagesAndComponents_AndListsEverySite()
	{
		var config = CatalogConfig();
		RemotePortalConfigService.AddDeclaredSites(config.sites, Declaring("player-profile-detail",
			Decl("player-profile-tabs", "route-tabs", "Tabs on a player's profile page.")));

		var options = ListPortalExtensionOptionsCommand.BuildOptions(config);

		Assert.That(options.pageExtensions.Single().routePrefix, Is.EqualTo("!hub/"));
		Assert.That(options.pageExtensions.Single().siteIds, Is.EqualTo(new[] { "extension-page" }));
		Assert.That(options.componentExtensions.Single().path, Is.EqualTo("players"));
		Assert.That(options.componentExtensions.Single().siteIds, Is.EqualTo(new[] { "nav-left-low" }));

		Assert.That(options.sites.Select(s => s.id),
			Is.EqualTo(new[] { "extension-page", "nav-left-low", "player-profile-tabs" }));
		var tabs = options.sites.Last();
		Assert.That(tabs.presentation, Is.EqualTo("route-tabs"));
		Assert.That(tabs.description, Is.EqualTo("Tabs on a player's profile page."));
		Assert.That(tabs.renderedBy, Is.EqualTo(new[] { "player-profile-detail" }));
	}
}
