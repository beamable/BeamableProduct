using System;
using Beamable.Common.Api.Notifications;

namespace Beamable.Tests.Runtime.Player.Notifications
{
   public class MockNotificationService : INotificationService
   {
      public void Subscribe(string name, Action<object> callback)
      {

      }

      public void Unsubscribe(string name, Action<object> handler)
      {

      }

      public void Publish(string name, object payload)
      {

      }
   }
}