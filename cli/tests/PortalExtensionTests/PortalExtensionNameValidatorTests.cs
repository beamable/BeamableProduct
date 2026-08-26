using cli.Services.PortalExtension;
using NUnit.Framework;
using System;
using System.Linq;

namespace tests.PortalExtensionTests;

public class PortalExtensionNameValidatorTests
{
	[Test]
	public void NoConflicts_WhenNamesAreUnique()
	{
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: new[] { "ExtA", "ExtB" },
			deployedPortalExtensions: new[] { "ExtC" },
			serviceAndStorageNames: new[] { "ServiceA", "StorageA" });

		Assert.That(conflicts, Is.Empty);
	}

	[Test]
	public void DeployedExtensionMatchingLocalExtension_IsNotAConflict()
	{
		// The same name appearing locally and deployed is an update of one extension, not a collision.
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: new[] { "ExtA" },
			deployedPortalExtensions: new[] { "ExtA" },
			serviceAndStorageNames: Array.Empty<string>());

		Assert.That(conflicts, Is.Empty);
	}

	[Test]
	public void DuplicateLocalExtensions_AreReported()
	{
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: new[] { "Dup", "Dup" },
			deployedPortalExtensions: Array.Empty<string>(),
			serviceAndStorageNames: Array.Empty<string>());

		Assert.That(conflicts.Count, Is.EqualTo(1));
		Assert.That(conflicts[0], Does.Contain("Dup"));
	}

	[Test]
	public void ExtensionNameMatchingMicroserviceOrStorage_IsReported()
	{
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: new[] { "Shared" },
			deployedPortalExtensions: Array.Empty<string>(),
			serviceAndStorageNames: new[] { "Shared" });

		Assert.That(conflicts.Count, Is.EqualTo(1));
		Assert.That(conflicts[0], Does.Contain("Shared"));
	}

	[Test]
	public void DeployedExtensionNameMatchingService_IsReported()
	{
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: Array.Empty<string>(),
			deployedPortalExtensions: new[] { "Shared" },
			serviceAndStorageNames: new[] { "Shared" });

		Assert.That(conflicts.Count, Is.EqualTo(1));
		Assert.That(conflicts[0], Does.Contain("Shared"));
	}

	[Test]
	public void ConflictDetection_IsCaseInsensitive()
	{
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: new[] { "MyExt" },
			deployedPortalExtensions: Array.Empty<string>(),
			serviceAndStorageNames: new[] { "myext" });

		Assert.That(conflicts, Is.Not.Empty);
	}

	[Test]
	public void NullAndEmptyNames_AreIgnored()
	{
		var conflicts = PortalExtensionNameValidator.FindConflicts(
			localPortalExtensions: new[] { null, "", "ExtA" },
			deployedPortalExtensions: new string[] { null },
			serviceAndStorageNames: new[] { "", null, "ServiceA" });

		Assert.That(conflicts, Is.Empty);
	}
}
