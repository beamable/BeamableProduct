using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Dependencies;

namespace Beamable.Server.Api.Notifications
{
    /// <summary>
    /// Microservice API for sending Notifications to clients.  
    /// </summary>
    public interface IMicroserviceNotificationsApi
    {
        /// <summary>
        /// Notifies the player with the given <paramref name="dbid"/> at the given <paramref name="context"/>. The <paramref name="context"/> is the one you should subscribe to in
        /// your <see cref="INotificationService.Subscribe"/> calls in the client-code. 
        /// </summary>
        /// <param name="dbid">The DBID for the player you wish to notify.</param>
        /// <param name="context">The context that player's client must be subscribed too to see the notification.</param>
        /// <param name="messagePayload">The non-JSON string data to send along with the notification.</param>
        Promise<EmptyResponse> NotifyPlayer(long dbid, string context, string messagePayload);

        /// <summary>
        /// Notifies the players identified by the given <paramref name="dbids"/> at the given <paramref name="context"/>. The <paramref name="context"/> is the one you should subscribe to in
        /// your <see cref="INotificationService.Subscribe"/> calls in the client-code. 
        /// </summary>
        /// <param name="dbids">The list of DBID for the players you wish to notify.</param>
        /// <param name="context">The context that player's client must be subscribed too to see the notification.</param>
        /// <param name="messagePayload">The non-JSON string data to send along with the notification.</param>
        Promise<EmptyResponse> NotifyPlayer(List<long> dbids, string context, string messagePayload);

        /// <summary>
        /// Notifies the player with the given <paramref name="dbids"/> at the given <paramref name="context"/>. The <paramref name="context"/> is the one you should subscribe to in
        /// your <see cref="INotificationService.Subscribe"/> calls in the client-code. 
        /// </summary>
        /// <param name="dbid">The DBID for the player you wish to notify.</param>
        /// <param name="context">The context that player's client must be subscribed too to see the notification.</param>
        /// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
        Promise<EmptyResponse> NotifyPlayer<T>(long dbid, string context, T messagePayload);

        /// <summary>
        /// Notifies the players identified by the given <paramref name="dbids"/> at the given <paramref name="context"/>. The <paramref name="context"/> is the one you should subscribe to in
        /// your <see cref="INotificationService.Subscribe"/> calls in the client-code. 
        /// </summary>
        /// <param name="dbids">The list of DBID for the players you wish to notify.</param>
        /// <param name="context">The context that player's client must be subscribed too to see the notification.</param>
        /// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
        Promise<EmptyResponse> NotifyPlayer<T>(List<long> dbids, string context, T messagePayload);
    }

}
