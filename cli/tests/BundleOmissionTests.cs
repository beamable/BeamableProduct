using Beamable.Common.BeamCli.Contracts;
using cli;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace tests;

/// <summary>
/// Bundles stays omitted from the CLI and from the Unity SDK on this release line.
///
/// These tests are red on purpose. They exist because intent that lives only in a branch name or a
/// note does not survive a pin bump: 6.1.0-PREVIEW.RC2 shipped a public <c>beam bundles</c> command
/// group and put <c>BundleTagInfo</c> back into the published com.beamable tarball, even though the
/// release branch was meant to withhold the feature. Making these green is the source-level rip-out.
///
/// They assert against the built command tree and the compiled assemblies rather than file paths,
/// so they hold no matter how the source is arranged, and they fail if the surface returns by any
/// route -- a cherry-pick, a regenerated interface, or a re-registration.
///
/// When Bundles ships for real, delete this file in the same change that reintroduces the feature.
/// </summary>
public class BundleOmissionTests
{
	/// <summary>
	/// The loadable types of an assembly. The CLI assembly references MSBuild types that are not
	/// present at test runtime, so a bare GetTypes() throws before any bundle type is reached.
	/// </summary>
	private static IEnumerable<Type> LoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			return exception.Types.Where(type => type is not null)!;
		}
	}

	private static string[] RootCommandNames()
	{
		var app = new App();
		app.Configure();
		app.Build();
		return app.InstantiateAllCommands().Select(command => command.Name).ToArray();
	}

	[Test]
	public void CommandTree_ExposesNoBundlesCommandGroup()
	{
		Assert.That(RootCommandNames(), Does.Not.Contain("bundles"),
			"`beam bundles` must not exist while the feature is withheld");
	}

	[Test]
	public void CommandTree_ExposesNoBundleForceInjectCommand()
	{
		// `beam deployment admin force-inject` is the fourteenth bundle command, registered outside
		// the BundleCommands namespace, which is why an audit of that directory alone misses it.
		Assert.That(RootCommandNames(), Does.Not.Contain("force-inject"),
			"the bundle force-injection admin command must not exist while the feature is withheld");
	}

	[Test]
	public void CliAssembly_ContainsNoBundleCommandOrServiceTypes()
	{
		var withheldNamespaces = new[] { "cli.BundleCommands", "cli.Services.Bundles" };

		var offenders = LoadableTypes(typeof(App).Assembly)
			.Where(type => !type.IsNested)
			.Where(type => type.Namespace != null && withheldNamespaces.Contains(type.Namespace))
			.Select(type => type.FullName)
			.OrderBy(name => name)
			.ToList();

		Assert.That(offenders, Is.Empty,
			$"bundle command and service types must not be compiled into the CLI; found: {string.Join(", ", offenders)}");
	}

	[Test]
	public void CommonAssembly_ContainsNoBundleContractTypes()
	{
		// This is the one that reaches customers. Everything in this assembly is packed into the
		// Beamable.Common nuget's content/sourceCode and written straight into the Unity package by
		// `beam unity download-all-nuget-packages`, so a contract type here ships in the SDK
		// regardless of what the Unity tree has committed.
		var commonAssembly = typeof(EnvironmentVersionData).Assembly;

		// Scoped to the CLI contract namespace on purpose: the generated OpenAPI bundle models under
		// Beamable.Common.OpenApi mirror the platform spec and are regenerated wholesale, so they are
		// deliberately left in place. UserBundle in the auth API is unrelated to this feature.
		var offenders = LoadableTypes(commonAssembly)
			.Where(type => type.Namespace == "Beamable.Common.BeamCli.Contracts" && type.Name.Contains("Bundle"))
			.Select(type => type.FullName)
			.OrderBy(name => name)
			.ToList();

		Assert.That(offenders, Is.Empty,
			$"bundle contract types must not ship in the Beamable.Common nuget; found: {string.Join(", ", offenders)}");
	}
}
