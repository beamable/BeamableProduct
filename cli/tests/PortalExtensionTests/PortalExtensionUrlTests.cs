using cli;
using cli.Services;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace tests.PortalExtensionTests;

public class PortalExtensionUrlTests
{
	private static IAppContext Context(string host, string cid = "1234", string pid = "DE_5678", string refreshToken = "tok")
	{
		var ctx = new Mock<IAppContext>();
		ctx.Setup(x => x.Host).Returns(host);
		ctx.Setup(x => x.Cid).Returns(cid);
		ctx.Setup(x => x.Pid).Returns(pid);
		ctx.Setup(x => x.RefreshToken).Returns(refreshToken);
		return ctx.Object;
	}

	private static PortalExtensionDef ExtensionWithMount(string page)
	{
		return new PortalExtensionDef
		{
			Name = "MyExtension",
			Properties = new PortalExtensionPackageProperties
			{
				Mounts = new List<PortalExtensionMountProperties>
				{
					new PortalExtensionMountProperties { Page = page }
				}
			}
		};
	}

	[Test]
	public void PortalUrlOverride_WinsOverHostTransform()
	{
		// When running against a local backend the resolved portal base url (which already honors
		// --portal-url) must be used verbatim instead of string-replacing the API host.
		var ctx = Context("http://localhost:8080");
		var ext = ExtensionWithMount("campaigns");

		var url = BeamoLocalSystem.BuildPortalExtensionUrl(ctx, ext, portalBaseUrl: "http://localhost:4950");

		Assert.That(url, Is.EqualTo("http://localhost:4950/1234/games/DE_5678/realms/DE_5678/campaigns?refresh_token=tok"));
	}

	[Test]
	public void NoOverride_CloudHost_StillMapsApiToPortal()
	{
		// Cloud behavior must remain identical: with no override, the api host is transformed to portal.
		var ctx = Context("https://api.beamable.com");
		var ext = ExtensionWithMount("campaigns");

		var url = BeamoLocalSystem.BuildPortalExtensionUrl(ctx, ext, portalBaseUrl: null);

		Assert.That(url, Is.EqualTo("https://portal.beamable.com/1234/games/DE_5678/realms/DE_5678/campaigns?refresh_token=tok"));
	}

	[Test]
	public void NoOverride_DevHost_MapsToDevPortal()
	{
		var ctx = Context("https://dev.api.beamable.com");
		var ext = ExtensionWithMount("campaigns");

		var url = BeamoLocalSystem.BuildPortalExtensionUrl(ctx, ext, portalBaseUrl: null);

		Assert.That(url, Is.EqualTo("https://dev-portal.beamable.com/1234/games/DE_5678/realms/DE_5678/campaigns?refresh_token=tok"));
	}
}
