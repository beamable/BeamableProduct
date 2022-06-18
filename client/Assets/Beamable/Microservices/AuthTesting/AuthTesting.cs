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
			var path = Path.Combine(basePath, "./View/dist/bundle.js");
			var javascript = File.ReadAllText(path);
			return "<script>" + javascript + "</script>";
		}
	}
}
