using System.Collections;
using System.Collections.Generic;
using Beamable.Common.Api;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Server.Tests.Runtime
{
   public class RequestTests : BeamableTest
   {
      private const string ROUTE = "test";

      [UnityTest]
      public IEnumerator CanDeserializeList()
      {
         var client = new TestClient(ROUTE);

         MockRequester.MockRequest<List<int>>(Method.POST,
               client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
            .WithRawResponse("[1,2,3,4,5]");

         var req = client.Request<List<int>>( ROUTE, new string[] { });

         yield return req.ToYielder();
         Assert.AreEqual(new List<int>{1,2,3,4,5}, req.GetResult());
      }

      [UnityTest]
      public IEnumerator CanDeserializeBoolean()
      {
         var client = new TestClient(ROUTE);

         MockRequester.MockRequest<bool>(Method.POST,
               client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
            .WithRawResponse("true");

         var req = client.Request<bool>( ROUTE, new string[] { });

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

         var req = client.Request<int>( ROUTE, new string[] { });

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

         var req = client.Request<string>( ROUTE, new string[] { });

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

         var req = client.Request<Vector2>( ROUTE, new string[] { });

         yield return req.ToYielder();
         Assert.AreEqual(new Vector2(1,3), req.GetResult());
      }
   }
}