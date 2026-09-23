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
}
