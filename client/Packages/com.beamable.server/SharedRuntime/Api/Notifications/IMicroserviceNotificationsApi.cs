// this file was copied from nuget package Beamable.Server.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.0-PREVIEW.RC6

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
		/// <param name="gamertag">The player id for the player you wish to notify.</param>
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
		/// <param name="gamertags">The list of player ids for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyPlayer(List<long> gamertags, string name, string messagePayload);

		/// <summary>
		/// Notifies the player with the given <paramref name="gamertag"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertag">The player id for the player you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyPlayer<T>(long gamertag, string name, T messagePayload);

		/// <summary>
		/// Notifies the players identified by the given <paramref name="gamertags"/> at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="gamertags">The list of player ids for the players you wish to notify.</param>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyPlayer<T>(List<long> gamertags, string name, T messagePayload);

		/// <summary>
		/// Notifies all players at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">
		/// The non-JSON string data to send along with the notification.
		/// Due to Beamable constraints, note that the string message will be sent with an outer "stringValue" field wrapping it.
		/// </param>
		Promise<EmptyResponse> NotifyGame(string name, string messagePayload);
		/// <summary>
		/// Notifies all players at the given <paramref name="name"/>. The <paramref name="name"/> is the one you should subscribe to in
		/// your <see cref="INotificationService.Subscribe{T}"/> calls in the client-code.
		/// </summary>
		/// <param name="name">The context that player's client must be subscribed too to see the notification.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		Promise<EmptyResponse> NotifyGame<T>(string name, T messagePayload);

		/// <summary>
		/// Notifies all servers at the given <paramref name="name"/>.
		/// </summary>
		/// <param name="toAll">When true, all subscribed servers will get the message. Otherwise, only 1 server will handle the message. </param>
		/// <param name="name">The name of the event that the server must listen for.</param>
		/// <param name="messagePayload">The data to send along with the notification. Must be a JSON-serializable type.</param>
		/// <returns></returns>
		Promise<EmptyResponse> NotifyServer(bool toAll, string name, string messagePayload);

		/// <inheritdoc cref="NotifyServer(bool,string,string)"/>
		Promise<EmptyResponse> NotifyServer(bool toAll, string name);

		/// <inheritdoc cref="NotifyServer(bool,string,string)"/>
		Promise<EmptyResponse> NotifyServer<T>(bool toAll, string name, T messagePayload);
	}
}
