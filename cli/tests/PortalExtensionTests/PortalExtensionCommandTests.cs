using cli;
using cli.Portal;
using cli.Services;
using cli.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using tests.Examples;
using tests.MoqExtensions;

namespace tests.PortalExtensionTests;

[NonParallelizable]
public class PortalExtensionCommandTests : CLITestExtensions
{
	private void MockRemotePortalConfig()
	{
		Mock<IRemotePortalConfigService>(mock =>
		{
			mock.Setup(x => x.GetRemotePortalConfig(It.IsAny<CommandArgs>()))
				.ReturnsAsync(GetTestPortalConfig());
		});
	}

	private static RemotePortalConfiguration GetTestPortalConfig()
	{
		return new RemotePortalConfiguration
		{
			mountSites = new List<RemotePortalConfiguration.MountSiteConfig>
			{
				new()
				{
					path = "!pathMatch",
					selectors = new List<RemotePortalConfiguration.MountSiteSelector>
					{
						new() { selector = "#extension-page", type = "page" }
					}
				},
				new()
				{
					path = "extensions",
					selectors = new List<RemotePortalConfiguration.MountSiteSelector>
					{
						new() { selector = "#realm-extensions", type = "component" }
					}
				},
				new()
				{
					path = "players",
					selectors = new List<RemotePortalConfiguration.MountSiteSelector>
					{
						new() { selector = "#top", type = "component" },
						new() { selector = "#bottom", type = "component" }
					},
					navContext = new List<string> { "Engage", "Players" }
				},
				new()
				{
					path = "players/:playerId",
					selectors = new List<RemotePortalConfiguration.MountSiteSelector>
					{
						new() { selector = "#top", type = "component" }
					},
					navContext = new List<string> { "Engage", "Players", "Player Profile" }
				},
				new()
				{
					path = "players/:playerId/!pathMatch",
					selectors = new List<RemotePortalConfiguration.MountSiteSelector>
					{
						new() { selector = "#extension-page", type = "page" }
					}
				}
			}
		};
	}

	private void MockRemotePortalConfigComponentOnly()
	{
		Mock<IRemotePortalConfigService>(mock =>
		{
			mock.Setup(x => x.GetRemotePortalConfig(It.IsAny<CommandArgs>()))
				.ReturnsAsync(new RemotePortalConfiguration
				{
					mountSites = new List<RemotePortalConfiguration.MountSiteConfig>
					{
						new()
						{
							path = "players",
							selectors = new List<RemotePortalConfiguration.MountSiteSelector>
							{
								new() { selector = "#top", type = "component" },
								new() { selector = "#bottom", type = "component" }
							},
							navContext = new List<string> { "Engage", "Players" }
						}
					}
				});
		});
	}

	// A catalog whose page slot uses the "!hub/!pathMatch" form — the page path a user supplies must
	// be stored verbatim, never prefixed with "!hub/".
	private void MockRemotePortalConfigHubPage()
	{
		Mock<IRemotePortalConfigService>(mock =>
		{
			mock.Setup(x => x.GetRemotePortalConfig(It.IsAny<CommandArgs>()))
				.ReturnsAsync(new RemotePortalConfiguration
				{
					mountSites = new List<RemotePortalConfiguration.MountSiteConfig>
					{
						new()
						{
							path = "!hub/!pathMatch",
							selectors = new List<RemotePortalConfiguration.MountSiteSelector>
							{
								new() { selector = "#extension-page", type = "page" }
							}
						}
					}
				});
		});
	}

	private void InitWorkspace()
	{
		SetupMocks(mockBeamoManifest: false, mockAdminMe: false);
		Ansi.Input.PushTextWithEnter(alias);
		Ansi.Input.PushTextWithEnter(userName);
		Ansi.Input.PushTextWithEnter(password);
		Ansi.Input.PushKey(ConsoleKey.Enter);
		Ansi.Input.PushKey(ConsoleKey.Enter);

		Run("init", "--save-to-file");
		_mockObjects.Clear();
		ResetConfigurator();
	}

	private void SetupBeamoServiceMock()
	{
		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
	}

	#region portal extension check

	[Test]
	public void PortalExtensionCheck_Succeeds_WhenDependenciesInstalled()
	{
		InitWorkspace();

		Run("portal", "extension", "check", "--quiet");
	}

	#endregion

	#region portal extension list-mount-sites

	[Test]
	public void ListMountSites_ReturnsConfigFromService()
	{
		InitWorkspace();
		SetupBeamoServiceMock();
		MockRemotePortalConfig();

		Run("portal", "extension", "list-mount-sites", "--quiet");
	}

	#endregion

	#region portal extension list-extension-options

	[Test]
	public void ListExtensionOptions_CategorizesPageAndComponentExtensions()
	{
		InitWorkspace();
		SetupBeamoServiceMock();
		MockRemotePortalConfig();

		Run("portal", "extension", "list-extension-options", "--quiet");
	}

	#endregion

	#region portal extension set-config

	[Test]
	public void SetConfig_SavesFileExtensionsToObserve()
	{
		InitWorkspace();

		Run("portal", "extension", "set-config", "--quiet",
			"--file-extensions-to-observe", ".md", ".json");

		var configContent = BFile.ReadAllText(".beamable/config.beam.json");
		Assert.That(configContent, Does.Contain("portalExtension"),
			"config must contain portalExtension section");
		Assert.That(configContent, Does.Contain(".md"),
			"config must contain .md extension");
		Assert.That(configContent, Does.Contain(".json"),
			"config must contain .json extension");
	}

	#endregion

	#region project new portal-extension (page)

	[Test]
	[TestCase("react")]
	public void NewPortalExtension_PageExtension_CreatesFiles(string template)
	{
		InitWorkspace();
		SetupBeamoServiceMock();
		MockRemotePortalConfig();

		Run("project", "new", "portal-extension", "TestPageExt", "--quiet",
			"--mount-page", "my-custom-page",
			"--mount-group", "TestGroup",
			"--mount-label", "TestLabel",
			"--template", template);

		Assert.That(BFile.Exists("extensions/TestPageExt/package.json"),
			"package.json must exist after scaffolding");

		var packageJson = BFile.ReadAllText("extensions/TestPageExt/package.json");
		Assert.That(packageJson, Does.Contain("my-custom-page"),
			"package.json must contain the mount page");
		Assert.That(packageJson, Does.Contain("#extension-page"),
			"package.json must contain the auto-assigned selector for page extensions");
		Assert.That(packageJson, Does.Contain("TestGroup"),
			"package.json must contain the nav group");
		Assert.That(packageJson, Does.Contain("TestLabel"),
			"package.json must contain the nav label");
	}

	[Test]
	public void NewPortalExtension_PageExtension_PassesThroughHubPath()
	{
		InitWorkspace();
		SetupBeamoServiceMock();
		MockRemotePortalConfigHubPage();

		Run("project", "new", "portal-extension", "FerrariExt", "--quiet",
			"--mount-page", "cars/ferrari",
			"--mount-group", "Cars",
			"--mount-label", "Ferrari",
			"--template", "react");

		var packageJson = BFile.ReadAllText("extensions/FerrariExt/package.json");
		Assert.That(packageJson, Does.Contain("cars/ferrari"),
			"the page path must be stored verbatim");
		Assert.That(packageJson, Does.Not.Contain("!hub"),
			"the !hub/!pathMatch prefix must not leak into the stored page path");
		Assert.That(packageJson, Does.Contain("#extension-page"),
			"the page slot selector must be auto-assigned");
	}

	#endregion

	#region project new portal-extension (component)

	[Test]
	public void NewPortalExtension_ComponentExtension_CreatesFiles()
	{
		InitWorkspace();
		SetupBeamoServiceMock();
		MockRemotePortalConfigComponentOnly();

		Run("project", "new", "portal-extension", "TestCompExt", "--quiet",
			"--mount-page", "players",
			"--mount-selector", "#top",
			"--template", "react");

		Assert.That(BFile.Exists("extensions/TestCompExt/package.json"),
			"package.json must exist after scaffolding");

		var packageJson = BFile.ReadAllText("extensions/TestCompExt/package.json");
		Assert.That(packageJson, Does.Contain("players"),
			"package.json must contain the mount page");
		Assert.That(packageJson, Does.Contain("#top"),
			"package.json must contain the selector");
	}

	#endregion

	#region portal extension add-microservice

	[Test]
	public void AddMicroservice_AddsDepToPackageJson()
	{
		InitWorkspace();

		SetupBeamoServiceMock();
		Run("project", "new", "service", "TestMs", "--quiet");
		_mockObjects.Clear();
		ResetConfigurator();

		SetupBeamoServiceMock();
		MockRemotePortalConfig();
		Run("project", "new", "portal-extension", "TestExt", "--quiet",
			"--mount-page", "my-ext-page",
			"--mount-group", "TestGroup",
			"--mount-label", "TestLabel",
			"--template", "react");
		_mockObjects.Clear();
		ResetConfigurator();

		SetupBeamoServiceMock();
		Run("portal", "extension", "add-microservice", "TestExt", "TestMs", "--quiet");

		var packageJson = BFile.ReadAllText("extensions/TestExt/package.json");
		Assert.That(packageJson, Does.Contain("TestMs"),
			"package.json must list TestMs as a microservice dependency");
	}

	#endregion

	#region project new portal-extension-lib

	[Test]
	public void NewPortalExtensionLib_CreatesFiles()
	{
		InitWorkspace();
		SetupBeamoServiceMock();

		Run("project", "new", "portal-extension-lib", "TestLib", "--quiet");

		Assert.That(BFile.Exists("extensions-libs/TestLib/package.json"),
			"package.json must exist after scaffolding the library");
		Assert.That(BFile.Exists("extensions-libs/TestLib/src/index.ts"),
			"src/index.ts must exist after scaffolding the library");

		var packageJson = BFile.ReadAllText("extensions-libs/TestLib/package.json");
		Assert.That(packageJson, Does.Contain("TestLib"),
			"package.json name must match the provided library name");
		Assert.That(packageJson, Does.Contain("portalExtensionLib"),
			"package.json must mark the project as a portal extension library");
		Assert.That(packageJson, Does.Contain("./src/index.ts"),
			"package.json must expose its TypeScript source as the entry point");
	}

	#endregion

	#region portal extension add-library

	[Test]
	public void AddLibrary_AddsFileDependencyToPackageJson()
	{
		InitWorkspace();

		SetupBeamoServiceMock();
		Run("project", "new", "portal-extension-lib", "TestLib", "--quiet");
		_mockObjects.Clear();
		ResetConfigurator();

		SetupBeamoServiceMock();
		MockRemotePortalConfig();
		Run("project", "new", "portal-extension", "TestExt", "--quiet",
			"--mount-page", "my-ext-page",
			"--mount-group", "TestGroup",
			"--mount-label", "TestLabel",
			"--template", "react");
		_mockObjects.Clear();
		ResetConfigurator();

		SetupBeamoServiceMock();
		Run("portal", "extension", "add-library", "TestLib", "--extensions", "TestExt", "--quiet");

		var packageJson = BFile.ReadAllText("extensions/TestExt/package.json");
		Assert.That(packageJson, Does.Contain("TestLib"),
			"package.json must list TestLib as a dependency");
		Assert.That(packageJson, Does.Contain("file:"),
			"package.json must reference the library via a file: specifier");
		Assert.That(packageJson, Does.Contain("extensions-libs/TestLib"),
			"the file: specifier must point at the library directory");
	}

	#endregion

	#region project new portal-extension (name conflicts)

	[Test]
	public void NewPortalExtension_Fails_WhenNameConflictsWithExistingExtension()
	{
		InitWorkspace();

		SetupBeamoServiceMock();
		MockRemotePortalConfig();
		Run("project", "new", "portal-extension", "DupExt", "--quiet",
			"--mount-page", "my-ext-page",
			"--mount-group", "TestGroup",
			"--mount-label", "TestLabel",
			"--template", "react");
		_mockObjects.Clear();
		ResetConfigurator();

		// The name check runs before the remote portal config fetch and any prompts, so only the
		// pre-Handle manifest init (BeamoService) needs to be mocked here.
		SetupBeamoServiceMock();
		var exitCode = RunFull(new[]
		{
			"project", "new", "portal-extension", "DupExt", "--quiet",
			"--mount-page", "my-ext-page",
			"--mount-group", "TestGroup",
			"--mount-label", "TestLabel",
			"--template", "react"
		});

		Assert.That(exitCode, Is.EqualTo(1),
			"creating a portal extension whose name duplicates an existing extension must fail");
	}

	[Test]
	public void NewPortalExtension_Fails_WhenNameConflictsWithMicroservice()
	{
		InitWorkspace();

		SetupBeamoServiceMock();
		Run("project", "new", "service", "Collide", "--quiet");
		_mockObjects.Clear();
		ResetConfigurator();

		SetupBeamoServiceMock();
		var exitCode = RunFull(new[]
		{
			"project", "new", "portal-extension", "Collide", "--quiet",
			"--mount-page", "my-ext-page",
			"--mount-group", "TestGroup",
			"--mount-label", "TestLabel",
			"--template", "react"
		});

		Assert.That(exitCode, Is.EqualTo(1),
			"creating a portal extension whose name collides with a microservice must fail");
	}

	#endregion
}
