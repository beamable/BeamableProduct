// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿using Beamable.Serialization;

namespace Beamable.Api.Analytics
{
	/// <summary>
	/// Analytics event request.
	/// This is the request object which is used to send data to the Platform
	/// </summary>
	public class AnalyticsEventRequest : JsonSerializable.ISerializable
	{
		/// <summary>
		/// Gets the payload.
		/// The payload is a json string
		/// </summary>
		/// <value>The payload.</value>
		public string Payload
		{
			get { return _payload; }
		}

		private string _payload;

		/// <summary>
		/// Initializes a new instance of the <see cref="AnalyticsEventRequest"/> class.
		/// Used mostly for serialization
		/// </summary>
		public AnalyticsEventRequest() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="AnalyticsEventRequest"/> class.
		/// </summary>
		/// <param name="payload">Payload (json string)</param>
		public AnalyticsEventRequest(string payload)
		{
			_payload = payload;
		}

		/// <summary>
		/// Serialize the specified object back and forth from json.
		/// </summary>
		/// <param name="s">Serialization stream</param>
		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("payload", ref _payload);
		}

	}
}
