using Beamable.Api.Connectivity;
using Beamable.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Player
{

	public interface ISdkEventService
	{
		Promise Add(SdkEvent evt);
		void Process();
		SdkEventConsumer Register(string source, SdkEventHandler handler);
		void Unregister(SdkEventConsumer consumer);

	}

	[System.Serializable]
	public class SdkEventService : ISdkEventService
	{
		private readonly IConnectivityService _connectivityService;
		private List<SdkEvent> _events = new List<SdkEvent>();
		private List<SdkEvent> _phaseSpawnedEvents = new List<SdkEvent>();

		private Dictionary<string, List<SdkEventConsumer>> _sourceToConsumers =
		   new Dictionary<string, List<SdkEventConsumer>>();

		private Dictionary<SdkEvent, Promise> _eventToCompletion =
		   new Dictionary<SdkEvent, Promise>();

		private bool _isProcessing;

		public SdkEventService(IConnectivityService connectivityService)
		{
			_connectivityService = connectivityService;
		}

		public Promise Add(SdkEvent evt)
		{
			_eventToCompletion[evt] = new Promise();
			if (!_isProcessing)
			{
				_events.Add(evt);
				Process();
			}
			else
			{
				_phaseSpawnedEvents.Add(evt);
			}

			return _eventToCompletion[evt];
		}
		public void Process()
		{
			if (_isProcessing) return;
			_isProcessing = true;

			foreach (var evt in _events)
			{
				if (_sourceToConsumers.TryGetValue(evt.Source, out var consumers))
				{
					for (var i = consumers.Count - 1; i >= 0; i--)
					{
						try
						{
							var promise = consumers[i].Handler?.Invoke(evt);
							promise?.Merge(_eventToCompletion[evt]);
						}
						catch (Exception ex)
						{
							Debug.LogError("Failed to execute a Beamable SDK event");
							Debug.LogException(ex);
						}
					}
				}
			}

			_isProcessing = false;
			_events.Clear();

			if (_phaseSpawnedEvents.Count > 0)
			{
				_events.AddRange(_phaseSpawnedEvents);
				_phaseSpawnedEvents.Clear();
				Process();
			}
		}

		public SdkEventConsumer Register(string source, SdkEventHandler handler)
		{
			var consumer = new SdkEventConsumer
			{
				Source = source,
				Handler = handler,
				Service = this,
				RunLater = RunLater
			};
			if (!_sourceToConsumers.TryGetValue(source, out var consumers))
			{
				consumers = new List<SdkEventConsumer>();
			}

			consumers.Add(consumer);
			_sourceToConsumers[source] = consumers;
			return consumer;
		}

		public void Unregister(SdkEventConsumer consumer)
		{
			if (_sourceToConsumers.TryGetValue(consumer.Source, out var consumers))
			{
				consumers.Remove(consumer);
			}
		}

		private Promise RunLater(SdkEvent evt)
		{
			_connectivityService.OnReconnectOnce(() =>
			{
				Add(evt);
			});
			return Promise.Success;
		}
	}

	public delegate Promise SdkEventHandler(SdkEvent evt);

	public class SdkEventConsumer
	{
		public string Source { get; set; }
		public SdkEventHandler Handler { get; set; }
		public SdkEventService Service { get; set; }

		internal SdkEventHandler RunLater
		{
			get;
			set;
		}

		public void Unsubscribe()
		{
			Service.Unregister(this);
		}

		public Promise RunAfterReconnection(SdkEvent evt)
		{
			return RunLater(evt);
		}
	}

	[Serializable]
	public class SdkEvent
	{
		[SerializeField]
		private string _source; // TODO: Can this be an enum?

		[SerializeField]
		private string _event; // TODO: Can this be an enum?

		[SerializeField]
		private string[] _args;

		public string Source => _source;
		public string Event => _event;
		public string[] Args => _args;

		public SdkEvent()
		{

		}

		public SdkEvent(string source, string evt, params string[] args)
		{
			_source = source;
			_event = evt;
			_args = args;
		}
	}
}
