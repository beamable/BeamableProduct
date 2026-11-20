using System.Collections.Generic;
using cli;
using cli.Services;
using NUnit.Framework;
using ServiceLogs = Beamable.Common.Constants.Features.Services.Logs;

namespace tests;

public class PortalExtensionReadyMessageTests
{
	[Test]
	public void SiteOnlyExtension_NamesItsSites()
	{
		var extension = new PortalExtensionDef
		{
			Name = "child-extension",
			Properties = new PortalExtensionPackageProperties
			{
				Mounts = new List<PortalExtensionMountProperties>
				{
					new() { SiteIds = new List<string> { "parent-page-site" } }
				}
			}
		};

		var message = BeamoLocalSystem.BuildPortalExtensionReadyMessage(null, null, null, extension);

		Assert.That(message, Does.StartWith(ServiceLogs.PORTAL_EXTENSION_RUNNING));
		Assert.That(message, Does.Contain("parent-page-site"));
		Assert.That(message, Does.Not.Contain("Invalid Mount Path"));
	}
}
