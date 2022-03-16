using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Experimental.Api.Sim
{
	// **Top level driver for a deterministic simulation**

	/// <summary>
	/// This type defines the %Client main entry point for the %Multiplayer feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/multiplayer-feature">Multiplayer</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class SimClient
	{
		public int LogHash { get; private set; }
		public int StateHash { get; private set; }
		public long Ping { get; private set; }
		// The replicated authoritative log of all events and the current most recent frame
		private SimLog log = new SimLog();
		// Handler for all network activity
		public SimNetworkInterface Network;
		// The last frame that was sent to the Playback. This is always equal to or less than the log's latest frame
		private long _playbackFramePointer = 0;
		private long _virtualFramePointer = 0;
		private long _maxFrame = 0;
		// Last time the local simulation advanced
		private long _lastLocalSimulationTimestamp = 0;
		private long _lastVirtualSimulationTimestamp = 0;
		private long _lastNetworkTimestamp = 0;
		// How many ms before each time we try to advance the simulation locally
		private long _frameTickMs;
		// Number of frames desired for the network to be ahead of the live simulation
		private long _targetNetworkLead;
		private bool _first = true;
		// Seeded random from the initial event injected by the server
		private System.Random _random;


		// Sim behavior objects that need to be spawned
		private List<SimBehavior> _sbSpawns = new List<SimBehavior>();
		private List<SimBehavior> _sbSpawnsBuffer = new List<SimBehavior>();
		// Sim behavior objects that need to be removed
		private HashSet<SimBehavior> _sbRemoves = new HashSet<SimBehavior>();
		private List<SimBehavior> _sbRemovesBuffer = new List<SimBehavior>();
		// Live sim behavior objects that are part of the world
		private List<SimBehavior> _sbLive = new List<SimBehavior>();
		// Callback subscription management for simulation events
		public delegate void EventCallback<T>(T body);
		private Dictionary<string, List<EventCallback<string>>> _eventCallbacks = new Dictionary<string, List<EventCallback<string>>>();
		private List<EventCallback<string>> _curCallbacks = new List<EventCallback<string>>();

		public string ClientId
		{
			get { return Network.ClientId; }
		}

		public SimClient(SimNetworkInterface network, long framesPerSecond, long targetNetworkLead)
		{
			this.Network = network;
			this._frameTickMs = 1000 / framesPerSecond;
			this._targetNetworkLead = targetNetworkLead;
			_virtualFramePointer = targetNetworkLead;

			this.OnInit((seed) =>
			{
				_random = new System.Random(seed.GetHashCode());
			});
		}

		public SimLog.Snapshot TakeSnapshot()
		{
			return log.ToSnapshot();
		}

		public void RestoreSnapshot(SimLog.Snapshot snapshot)
		{
			_maxFrame = snapshot.frame;
			log.FromSnapshot(snapshot);
		}

		// Add an event to the log eventually. It won't be 'real' until it's in the log. The network decides when/how that happens
		public void SendEvent(object evt)
		{
			SendEvent(evt.GetType().ToString(), evt);
		}
		public void SendEvent(string name, object evt)
		{
			// TODO: Eliminate these allocations
			string raw = JsonUtility.ToJson(evt);
			Network.SendEvent(new SimEvent(Network.ClientId, name, raw));
		}

		public void Update()
		{
			if (_maxFrame > 0 && _playbackFramePointer == _maxFrame)
			{
				return;
			}

			if (!Network.Ready)
			{
				return;
			}
			if (_first)
			{
				_first = false;
				this._lastLocalSimulationTimestamp = GetTimeMs();
				this._lastVirtualSimulationTimestamp = GetTimeMs();
			}

			long logLead = log.Frame - _playbackFramePointer;
			long curTime = GetTimeMs();
			long deltaT = curTime - _lastLocalSimulationTimestamp;

			long virtualDeltaT = curTime - _lastVirtualSimulationTimestamp;
			while (virtualDeltaT >= _frameTickMs)
			{
				_virtualFramePointer += 1;
				if (virtualDeltaT >= _frameTickMs)
				{
					virtualDeltaT -= _frameTickMs;
				}
				_lastVirtualSimulationTimestamp = GetTimeMs();
			}

			// What frame rate should we use? If we're too far behind, let's speed it up
			// Shave 10% off for every frame we're behind
			long frameTickMs = _frameTickMs;
			long catchupThreshold = _targetNetworkLead * 2;
			if (logLead > catchupThreshold)
			{
				long severity = logLead - catchupThreshold;
				frameTickMs = (long)(frameTickMs - (frameTickMs * (severity * 0.1f)));
			}
			if (deltaT >= frameTickMs)
			{
				SimUpdate();
				_lastLocalSimulationTimestamp = curTime;
				frameTickMs += _frameTickMs;
			}
			// If we're really far behind, catch up *a lot*
			while (frameTickMs < 0)
			{
				SimUpdate();
				_lastLocalSimulationTimestamp = curTime;
				frameTickMs += _frameTickMs;
			}

			var tickFrames = Network.Tick(_playbackFramePointer, log.Frame, _virtualFramePointer);
			for (int i = 0; i < tickFrames.Count; i++)
			{
				SimFrame frame = tickFrames[i];
				log.ApplyFrame(frame);
				Ping = curTime - _lastNetworkTimestamp;
				_lastNetworkTimestamp = curTime;
			}
		}

		private long GetTimeMs()
		{
			return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		}

		private void SimUpdate()
		{
			// Stop advancing if there's no where to go
			if (_playbackFramePointer >= log.Frame)
			{
				return;
			}

			// Trigger ticks and events for this time point
			TriggerEventCallbacks("tick", "$system", "" + _playbackFramePointer);
			List<SimEvent> events = log.GetEventsAtFrame(_playbackFramePointer);
			for (int i = 0; i < events.Count; i++)
			{
				SimEvent evt = events[i];
				TriggerEventCallbacks(evt.Type, evt.Origin, evt.Body);
			}
			_playbackFramePointer += 1;

			// Flush any queued spawns / destroys / etc.
			FlushSimBehaviors();

			/*if (_playbackFramePointer % 200 == 0) {
			   DumpLog();
			}*/
		}

		// Manage simulation entities
		public T Spawn<T>(SimBehavior original, string id = "")
		{
			SimBehavior result = MonoBehaviour.Instantiate(original) as SimBehavior;
			this._sbSpawns.Add(result);
			result.SimInit(this, id);
			return result.GetComponent<T>();
		}

		public void RemoveSimBehavior(SimBehavior simObj)
		{
			this._sbRemoves.Add(simObj);
		}

		public EventCallback<string> On<T>(string evt, string origin, EventCallback<T> callback)
		{
			return OnInternal(evt, origin, (raw) =>
			{
				callback(JsonUtility.FromJson<T>(raw));
			});
		}
		private EventCallback<string> OnInternal(string evt, string origin, EventCallback<string> callback)
		{
			List<EventCallback<string>> callbacks;
			if (!_eventCallbacks.TryGetValue(evt + ":" + origin, out callbacks))
			{
				callbacks = new List<EventCallback<string>>();
				_eventCallbacks.Add(evt + ":" + origin, callbacks);
			}
			callbacks.Add(callback);
			return callback;
		}

		public void Remove(EventCallback<string> callback)
		{
			foreach (KeyValuePair<string, List<EventCallback<string>>> entry in _eventCallbacks)
			{
				entry.Value.RemoveAll((item) => item == callback);
			}
		}

		public EventCallback<string> OnInit(EventCallback<string> callback) { return OnInternal("init", "$system", callback); }
		public EventCallback<string> OnConnect(EventCallback<string> callback) { return OnInternal("connect", "$system", callback); }
		public EventCallback<string> OnDisconnect(EventCallback<string> callback) { return OnInternal("disconnect", "$system", callback); }
		public EventCallback<string> OnTick(EventCallback<long> callback)
		{
			return OnInternal("tick", "$system", (raw) =>
			{
				callback(long.Parse(raw));
			});
		}

		private void FlushSimBehaviors()
		{
			// Copy lists since processing may spawn more (which will defer until next tick)
			_sbSpawnsBuffer.Clear();
			_sbSpawnsBuffer.AddRange(this._sbSpawns);
			this._sbSpawns.Clear();

			_sbRemovesBuffer.Clear();
			_sbRemovesBuffer.AddRange(this._sbRemoves);
			this._sbRemoves.Clear();

			for (int i = 0; i < _sbSpawnsBuffer.Count; i++)
			{
				SimBehavior next = _sbSpawnsBuffer[i];
				this._sbLive.Add(next);
				next.SimEnter();
			}

			for (int i = 0; i < _sbRemovesBuffer.Count; i++)
			{
				SimBehavior next = _sbRemovesBuffer[i];
				// Remove from ticks, unregister callback, destroy the game object
				this._sbLive.Remove(next);
				foreach (KeyValuePair<string, List<EventCallback<string>>> entry in _eventCallbacks)
				{
					entry.Value.RemoveAll((item) => next.EventCallbacks.Contains(item));
				}
				next.SimExit();
			}
		}

		private void TriggerEventCallbacks(string evt, string origin, string body)
		{
			if (evt.StartsWith("$"))
			{
				evt = evt.Substring(1);
				origin = "$system";
			}

			_curCallbacks.Clear();
			List<EventCallback<string>> callbacks;
			if (!_eventCallbacks.TryGetValue(evt + ":" + origin, out callbacks))
			{
				return;
			}

			// Invoke callbacks and guard against de-registration during callbacks
			_curCallbacks.AddRange(callbacks);
			for (int i = 0; i < _curCallbacks.Count; i++)
			{
				EventCallback<string> next = _curCallbacks[i];
				if (callbacks.Contains(next))
				{
					next(body);
				}
			}
		}

		private void DumpLog()
		{
			string strLog = log.ToString(_playbackFramePointer);
			Debug.Log(strLog);
			LogHash = strLog.GetHashCode();
			string stateHash = "";
			for (int i = 0; i < _sbLive.Count; i++)
			{
				stateHash = stateHash + _sbLive[i].StateHash();
			}
			StateHash = stateHash.GetHashCode();
			Debug.Log("HASH: " + LogHash + " " + StateHash);
			log.Prune(_playbackFramePointer - 1);
		}

		public int RandomInt()
		{
			return _random.Next();
		}
	}
}
