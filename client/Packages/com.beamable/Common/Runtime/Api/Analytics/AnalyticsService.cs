// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

using Beamable.Common.Api;
using Beamable.Common.Spew;
using Beamable.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Beamable.Api.Analytics
{
	public class AnalyticsService : IBeamAnalyticsService
	{
		private IBeamableRequester _requester;
		private IUserContext _context;
		StringBuilder _builder = new StringBuilder(string.Empty);
		
		public AnalyticsService(IUserContext context, IBeamableRequester requester)
		{
			_context = context;
			_requester = requester;
		}

		public void SendAnalyticsEvent(AnalyticsEventRequest eventRequest)
		{
			AnalyticsEventBatchRequest(new []{eventRequest.Payload});
		}

		public void SendAnalyticsEventBatch(List<AnalyticsEventRequest> eventBatch)
		{
			int batchCount = eventBatch.Count;
			if (batchCount == 0) return;
			var batch = new string[batchCount];

			for (int i = 0; i < batchCount; i++)
			{
				batch[i] = eventBatch[i].Payload;
			}
			AnalyticsEventBatchRequest(batch);
		}

		public AnalyticsEventRequest BuildRequest(IAnalyticsEvent analyticsEvent)
		{
			using (var jsonSaveStream = JsonSerializable.JsonSaveStream.Spawn())
			{
				// Start as Object and Serialize Directly, so that we can inject shard
				jsonSaveStream.Init(JsonSerializable.JsonSaveStream.JsonType.Object);
				analyticsEvent.Serialize(jsonSaveStream);
				// TODO: Double check that we don't support shards anymore.
				// string shard = _platform.Shard;
				// if(shard != null)
				// 	jsonSaveStream.Serialize("shard", ref shard);
				jsonSaveStream.Conclude();
				return new AnalyticsEventRequest(jsonSaveStream.ToString());
			}
		}

		/// <summary>
		/// Analytics Event Batch Request coroutine.
		/// This constructs the web request and sends it to the Platform
		/// </summary>
		/// <returns>The event batch request.</returns>
		/// <param name="eventBatch">Event batch.</param>
		void AnalyticsEventBatchRequest(IList<string> eventBatch)
		{
			var eventsAmount = eventBatch.Count;
			if (eventsAmount == 0)
				return;

			long gamerTag = _context.UserId;
			if (gamerTag == 0)
			{
				gamerTag = 1;
			}
			string uri = string.Format("/report/custom_batch/{0}/{1}/{2}", _requester.Cid, _requester.Pid, gamerTag);

			string batchJson;
			// using (var pooledBuilder = new StringBuilder("["))
			{
				_builder.Clear();
				_builder.Append('[');
				int i = 0;
				_builder.AppendFormat("{0}", eventBatch[i]);
				for (i = 1; i < eventBatch.Count; i++)
				{
					_builder.AppendFormat(",{0}", eventBatch[i]);
				}
				_builder.Append(']');

				batchJson = _builder.ToString();
			}

			AnalyticsLogger.LogFormat("AnalyticsService.AnalyticsEventBatchRequest: Sending batch of {0} to uri: {1}", eventBatch.Count, uri);

			_requester.Request(Method.POST, uri, body: batchJson, parser: s => s);
		}
	}
}
