using System;
namespace Beamable.Common.Api.Notifications
{
   public interface INotificationService
   {
      void Subscribe(string name, Action<object> callback);

      void Unsubscribe(string name, Action<object> handler);

      void Publish(string name, object payload);
   }

   public static class NotificationServiceExtensions
   {
      public static string GetRefreshTokenForService(this INotificationService _, string service) =>
         $"{service}.refresh";
   }
}