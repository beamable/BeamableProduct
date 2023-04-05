using Beamable.Server.Common.XmlDocs;
using NUnit.Framework;
using UnityEngine;

namespace microserviceTests.OpenAPITests;

/// <summary>
/// x
/// </summary>
public class XmlTests
{
	[Test]
	public void ReadComments()
	{
		var c =DocsLoader.GetTypeComments(typeof(XmlTests));
		Assert.AreEqual("x", c.Summary);
	}
}
