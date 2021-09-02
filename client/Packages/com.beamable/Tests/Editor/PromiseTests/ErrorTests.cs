using System;
using System.Collections;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Platform.SDK;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests.PromiseTests
{
   public class ErrorTests
   {

      [Test]
      public void UncaughtPromise_RaisesEvent()
      {
         var p = new Promise<int>();
         var knownEx = new Exception();

         var eventRan = false;
         PromiseBase.SetPotentialUncaughtErrorHandler((promise, err) => {
            Assert.AreEqual(err, knownEx);
            eventRan = true;
         });

         p.CompleteError(knownEx);

         Assert.IsTrue(eventRan);
      }

      [Test]
      public void CaughtPromise_Before_DoesntRaiseEvent()
      {
         var p = new Promise<int>();
         var knownEx = new Exception();

         var eventRan = false;
         PromiseBase.SetPotentialUncaughtErrorHandler( (promise, err) => {
            Assert.Fail("uncaught error");
         });

         p.Error(ex => {
            eventRan = true;
            Assert.AreEqual(knownEx, ex);
         }).CompleteError(knownEx);


         Assert.IsTrue(eventRan);
      }


      [UnityTest]
      public IEnumerator CaughtPromise_After_RaisesEvent_NoLog()
      {
         var p = new Promise<int>();
         var knownEx = new Exception();
         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         var eventRan = false;
         PromiseExtensions.RegisterUncaughtPromiseHandler();
         mockLogger.onException += exception =>
         {
            Assert.Fail("Log should not run");
         };

         p.CompleteError(knownEx);
         p.Error(ex =>
         {
            eventRan = true;
            Assert.AreEqual(knownEx, ex);
         });

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(eventRan);
      }

      [UnityTest]
      public IEnumerator UncaughtPromise_TriggersBeamLog()
      {
         var p = new Promise<int>();
         var knownEx = new Exception();
         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;
         var logRan = false;

         mockLogger.onException += exception =>
         {
            Assert.AreEqual(exception.InnerException, knownEx);
            logRan = true;
         };

         PromiseExtensions.RegisterUncaughtPromiseHandler();

         p.CompleteError(knownEx);

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(logRan);
      }

      [Test]
      public void ErrorOnFailedPromise()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         mockLogger.onException += exception =>
         {
            Assert.Fail("error log should not be called");
         };

         var knownEx = new Exception();
         var errorCallbackRan = false;
         Exception errorEx = null;
         var p = Promise<int>.Failed(knownEx).Error(ex =>
         {
            errorCallbackRan = true;
            errorEx = ex;
         });

         Assert.IsTrue(errorCallbackRan);
         Assert.AreEqual(knownEx, errorEx);
      }

      [UnityTest]
      public IEnumerator FlatMapAfterAFailedPromise_WithHandler_ShouldNotLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         mockLogger.onException += exception =>
         {
            Assert.Fail("error log should not be called");
         };
         PromiseExtensions.RegisterUncaughtPromiseHandler();


         var knownEx = new Exception();
         var errorCallbackRan = false;
         Exception errorEx = null;
         var p = Promise<int>.Failed(knownEx).FlatMap(Promise<int>.Successful).Error(ex =>
         {
            errorCallbackRan = true;
            errorEx = ex;
         });

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }


         Assert.IsTrue(errorCallbackRan);
         Assert.AreEqual(knownEx, errorEx);
      }

      [UnityTest]
      public IEnumerator FlatMapAfterAFailedPromise_WithNoHandler_ShouldLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;
         var knownEx = new Exception();
         var logRan = false;

         mockLogger.onException += exception =>
         {
            Assert.AreEqual(knownEx, exception.InnerException);
            logRan = true;
         };
         PromiseExtensions.RegisterUncaughtPromiseHandler();

         var p = Promise<int>.Failed(knownEx).FlatMap(Promise<int>.Successful);

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }


         Assert.IsTrue(logRan);
      }

      [UnityTest]
      public IEnumerator FlatMapOverAFailedPromise_WithHandler_ShouldNotLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         mockLogger.onException += exception =>
         {
            Assert.Fail("error log should not be called");
         };
         PromiseExtensions.RegisterUncaughtPromiseHandler();

         var knownEx = new Exception();
         var errorCallbackRan = false;
         Exception errorEx = null;
         var p = Promise<int>.Successful(0)
            .FlatMap(_ => Promise<int>.Failed(knownEx))
            .Error(ex =>
            {
               errorCallbackRan = true;
               errorEx = ex;
            });
         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(errorCallbackRan);
         Assert.AreEqual(knownEx, errorEx);
      }

      [UnityTest]
      public IEnumerator FlatMapOverAFailedPromise_WithNoHandler_ShouldLog()
      {
         var knownEx = new Exception();

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         var logRan = false;
         mockLogger.onException += exception =>
         {
            Assert.AreEqual(knownEx, exception.InnerException);
            logRan = true;
         };
         PromiseExtensions.RegisterUncaughtPromiseHandler();

         Promise<int>.Successful(0)
            .FlatMap(_ => Promise<int>.Failed(knownEx));

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(logRan);
      }


      [UnityTest]
      public IEnumerator MapOverException_WithHandler_ShouldNotLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         mockLogger.onException += exception =>
         {
            Assert.Fail("error log should not be called");
         };
         PromiseExtensions.RegisterUncaughtPromiseHandler();


         var knownEx = new Exception();
         var errorCallbackRan = false;
         var p = Promise<int>.Successful(0).Map<int>(_ => throw knownEx).Error(ex =>
         {
            errorCallbackRan = true;
            Assert.AreEqual(knownEx, ex);
         });

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(errorCallbackRan);
      }

      [UnityTest]
      public IEnumerator MapOverException_WithNoHandler_ShouldLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;
         var knownEx = new Exception();
         var logRan = false;

         mockLogger.onException += exception =>
         {
            logRan = true;
            Assert.AreEqual(knownEx, exception.InnerException);
         };
         PromiseExtensions.RegisterUncaughtPromiseHandler();

         var p = Promise<int>.Successful(0).Map<int>(_ => throw knownEx);

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(logRan);
      }

      [UnityTest]
      public IEnumerator MapAfterAFailedPromise_WithHandler_ShouldNotLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         PromiseExtensions.RegisterUncaughtPromiseHandler();

         mockLogger.onException += exception =>
         {
            Assert.Fail("error log should not be called");
         };

         var knownEx = new Exception();
         var errorCallbackRan = false;
         Exception errorEx = null;
         var p = Promise<int>.Failed(knownEx).Map(Promise<int>.Successful).Error(ex =>
         {
            errorCallbackRan = true;
            errorEx = ex;
         });

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }


         Assert.IsTrue(errorCallbackRan);
         Assert.AreEqual(knownEx, errorEx);
      }

      [UnityTest]
      public IEnumerator MapAfterAFailedPromise_WithNoHandler_ShouldLog()
      {

         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         PromiseExtensions.RegisterUncaughtPromiseHandler();
         var logRan = false;
         var knownEx = new Exception();

         mockLogger.onException += exception =>
         {
            logRan = true;
            Assert.AreEqual(knownEx, exception.InnerException);
         };

         var p = Promise<int>.Failed(knownEx).Map(Promise<int>.Successful);
         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(logRan);
      }

      [UnityTest]
      public IEnumerator RecoverWithAfterAFailedPromise_ShouldNotLog()
      {
         var mockLogger = new MockLogProvider();
         BeamableLogProvider.Provider = mockLogger;

         PromiseExtensions.RegisterUncaughtPromiseHandler();
         var knownEx = new Exception();
         var recoverRan = false;
         mockLogger.onException += exception =>
         {
            Assert.Fail("error log should not be called");
         };

         Promise<int>.Failed(knownEx).RecoverWith(ex =>
         {
            recoverRan = true;
            Assert.AreEqual(knownEx, ex);
            return Promise<int>.Successful(1);
         });

         var task = PromiseExtensions.WaitForAllUncaughtHandlers();
         while (!task.IsCompleted) { yield return null; }
         if (task.IsFaulted) { throw task.Exception; }

         Assert.IsTrue(recoverRan);
      }

   }
}