using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Server;
using NUnit.Framework;


namespace microserviceTests.microservice.dbmicroservice.MicroserviceRequesterTests
{
	[TestFixture]
	public class AddListenerTests : CommonTest
	{
		[Test]
		[TimeoutWithTeardown(3 * 60 * 1000)]
		public async Task MultiThreadedAccess()
		{
			var context = new SocketRequesterContext(() =>
				throw new NotImplementedException("This test should never access the socket"));

			const int threadCount = 500;
			const int cycleCount = 5000;

			const string uri = "uri";
			Func<string, object> dumbParser = (raw) => 1;

			Exception failure = null;

			Task<bool> Launch(int threadNumber)
			{
				var thread = Task.Run(async () =>
				{
					try
					{

						for (var i = 0; i < cycleCount; i++)
						{
							var id = (threadNumber * cycleCount) + i;
							var req = new WebsocketRequest { id = id };
							context.AddListener(req, uri, dumbParser, null);
							await Task.Yield();
						}

						return true;
					}
					catch (Exception ex)
					{
						failure = ex;
						return false;
					}
				});
				return thread;
			}

			var threads = new List<Task<bool>>();
			for (var i = 0; i < threadCount; i++)
			{
				threads.Add(Launch(i));
			}

			// wait for all threads to terminate...

			await Task.WhenAll(threads);

			if (failure != null)
			{
				Assert.Fail("Failed thread. " + failure.Message + " " + failure.StackTrace);
			}

		}
	}
}
