using Beamable.Common;
using Beamable.Microservices.View;
using Beamable.Server;
using System.IO;
using UnityEngine;

namespace Beamable.Microservices
{
	[Microservice("AuthTesting")]
	public class AuthTesting : Microservice
	{
		[Callable]
		public string CustomComponent()
		{
			var basePath = "authtesting/Beamable.Microservice.AuthTesting"; // TODO
			var path = Path.Combine(basePath, "./View/index.html");
			var html = File.ReadAllText(path);
			return html;
		}


		[InitializeServices]
		public static void Init(IServiceInitializer serviceInitializer)
		{
			Debug.Log("Running local init method");

			// build the local front-ends...




		}

	}
}
