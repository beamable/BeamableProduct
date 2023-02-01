using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;

namespace Beamable.Server.Api.Notifications
{
	/// <summary>
	/// Microservice API for sending Notifications to clients.
	/// </summary>
	public interface IMicroserviceNotificationsApi
	{
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">
		/// The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyPlayer(long gamertag, string name, string messagePayload);

		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyPlayer(List<long> gamertags, string name, string messagePayload);

		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyPlayer<T>(long gamertag, string name, T messagePayload);

		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyPlayer<T>(List<long> gamertags, string name, T messagePayload);
		
		
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">
		/// The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGlobal(long gamertag, string name, string messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGlobal(List<long> gamertags, string name, string messagePayload);
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGlobal<T>(long gamertag, string name, T messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGlobal<T>(List<long> gamertags, string name, T messagePayload);

		
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">
		/// The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyCustom(long gamertag, string name, string messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyCustom(List<long> gamertags, string name, string messagePayload);
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyCustom<T>(long gamertag, string name, T messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyCustom<T>(List<long> gamertags, string name, T messagePayload);
		
		
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">
		/// The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGeneric(long gamertag, string name, string messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGeneric(List<long> gamertags, string name, string messagePayload);
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGeneric<T>(long gamertag, string name, T messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGeneric<T>(List<long> gamertags, string name, T messagePayload);
		
		
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">
		/// The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGame(long gamertag, string name, string messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGame(List<long> gamertags, string name, string messagePayload);
		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The gamertag for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGame<T>(long gamertag, string name, T messagePayload);
		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of gamertags for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGame<T>(List<long> gamertags, string name, T messagePayload);
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="toAll"></param>
		/// <param name="eventName"></param>
		/// <param name="messagePayload"></param>
		/// <returns></returns>
		Promise<EmptyResponse> NotifyServer(bool toAll, string eventName, string messagePayload);
		
		// TODO-ARTUR - Add correct summary to each option 
	}
}
