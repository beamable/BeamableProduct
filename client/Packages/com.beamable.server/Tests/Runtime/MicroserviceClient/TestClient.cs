using Beamable.Common;
using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Server.Tests.Runtime
{

	public class TestClient : MicroserviceClient
	{
		public class TestPrefixProvider : IMicroservicePrefixService
		{
			public Promise<string> GetPrefix(string serviceName)
			{
				return Promise<string>.Successful("test");
			}
		}

		private readonly string _serviceName;

		private readonly IDependencyProvider _provider;
		public TestClient(string serviceName)
		{
			_serviceName = serviceName;
			MicroserviceClientHelper.SetPrefix("test");
			var builder = new DependencyBuilder();
			builder.AddSingleton<IMicroservicePrefixService, TestPrefixProvider>();
			_provider = builder.Build();
		}

		public override IDependencyProvider Provider => _provider;

		public Promise<T> Request<T>(string endpoint, string[] serializedFields)
		{
			return base.Request<T>(_serviceName, endpoint, serializedFields);
		}

		public Promise<T> Request<T>(string endpoint, Dictionary<string, object> serializedFields)
		{
			return base.Request<T>(_serviceName, endpoint, serializedFields);
		}

		public string GetMockPath(string cid, string pid, string endpoint)
		{
			return MicroserviceClientHelper.CreateUrl(cid, pid, _serviceName, endpoint, "test");
		}
	}

	public class TestJSON
	{
		public int a;
		public int b;
	}

	[Serializable]
	public class TestProperties
	{
		[field: SerializeField] public int A { get; set; }
		[field: SerializeField] public string B { get; set; }
	}
}
