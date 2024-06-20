using Beamable.Common;
using Beamable.Common.Api;
using NUnit.Framework;
using System.Threading.Tasks;

namespace microserviceTests.PromiseTests;

public class AsyncTests
{
	[Test]
	public async Task Test()
	{
		async Promise Method()
		{
			await Task.Delay(50);
		}
		await Method();

	}
}
