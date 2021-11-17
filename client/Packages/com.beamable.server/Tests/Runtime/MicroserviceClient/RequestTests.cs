using Beamable.Common.Api;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Server.Tests.Runtime
{
	public class RequestTests : BeamableTest
	{
		private const string ROUTE = "test";

		[UnityTest]
		public IEnumerator CanDeserializeList_OfInt()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<List<int>>(Method.POST,
												 client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("[1,2,3,4,5]");

			var req = client.Request<List<int>>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual(new List<int>
			{
				1,
				2,
				3,
				4,
				5
			}, req.GetResult());
		}

		[UnityTest]
		public IEnumerator CanDeserializeList_OfStrings()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<List<string>>(Method.POST,
													client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("[\"a\", \"b\", \"c\"]");

			var req = client.Request<List<string>>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual(new List<string> { "a", "b", "c" }, req.GetResult());
		}

		[UnityTest]
		public IEnumerator CanDeserializeList_OfTypedObjects()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<List<SimplePoco>>(Method.POST,
														client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("[{\"A\": 1}, {\"A\": 2}]");

			var req = client.Request<List<SimplePoco>>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual(new List<SimplePoco> { new SimplePoco { A = 1 }, new SimplePoco { A = 2 } }, req.GetResult());
		}

		[UnityTest]
		public IEnumerator CanNotDeserializePolymorphicList()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<List<object>>(Method.POST,
													client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("[3, {\"A\": 2}, \"b\", true]");

			var req = client.Request<List<object>>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.IsNull(req.GetResult());
		}

		[System.Serializable]
		public class SimplePoco
		{
			public int A;

			public override bool Equals(object obj)
			{
				return obj != null && obj is SimplePoco casted && casted.A == A;
			}

			public override int GetHashCode()
			{
				return A;
			}

			public override string ToString() => $"A=[{A}]";
		}

		[UnityTest]
		public IEnumerator CanDeserializeBoolean()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<bool>(Method.POST,
											client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("true");

			var req = client.Request<bool>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual(true, req.GetResult());
		}

		[UnityTest]
		public IEnumerator CanDeserializeInt()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<int>(Method.POST,
										   client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("31");

			var req = client.Request<int>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual(31, req.GetResult());
		}

		[UnityTest]
		public IEnumerator CanDeserializeString()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<string>(Method.POST,
											  client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("hello world");

			var req = client.Request<string>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual("hello world", req.GetResult());
		}

		[UnityTest]
		public IEnumerator CanDeserializeObject()
		{
			var client = new TestClient(ROUTE);

			MockRequester.MockRequest<Vector2>(Method.POST,
											   client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
						 .WithRawResponse("{\"x\": 1, \"y\": 3}");

			var req = client.Request<Vector2>(ROUTE, new string[] { });

			yield return req.ToYielder();
			Assert.AreEqual(new Vector2(1, 3), req.GetResult());
		}
	}
}
