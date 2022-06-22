using Beamable.Common;
using Beamable.Microservices.View;
using Beamable.Server;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Microservices
{
	[Microservice("AuthTesting")]
	public class AuthTesting : Microservice
	{
		[ClientCallable]
		public async Promise<int> Add(int a, int b)
		{
			await Task.Delay(100);
			return a + b + 1;
		}
	}
}
