using Beamable.Common;
using NUnit.Framework;
using System.Collections.Generic;

namespace microserviceTests.PromiseTests;

public class WhenAllTests
{
	[Test]
	public void Simple()
	{
		var pList = new List<Promise<int>>
		{
			new Promise<int>(),
			new Promise<int>(), 
			new Promise<int>(),
			new Promise<int>(),
		};

		var whenAll = Promise.WhenAll(pList);
		var ran = false;
		whenAll.Then(_ =>
		{
			for (var i = 0; i < pList.Count; i++)
			{
				Assert.That(pList[i].IsCompleted, $"promise index=[{i}] must be completed.");
			}

			ran = true;
		});

		foreach (var p in pList)
		{
			p.CompleteSuccess(1);
		}
		Assert.That(ran, Is.True);
	}
}
