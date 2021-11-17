using Beamable.Api;
using Beamable.Service;
using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Sim
{
	/// <summary>
	/// This type defines the %SimNetworkEventStream for the %Multiplayer feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.SimClient script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class SimNetworkEventStream : SimNetworkInterface
	{
		private static long REQ_FREQ_MS = 1000;

		public string ClientId
		{
			get;
			private set;
		}

		public bool Ready
		{
			get;
			private set;
		}

		private List<SimEvent> _eventQueue = new List<SimEvent>();
		private List<SimFrame> _syncFrames = new List<SimFrame>();
		private List<SimFrame> _emptyFrames = new List<SimFrame>();
		private SimFrame _nextFrame = new SimFrame(-1, new List<SimEvent>());
		private long _lastReqTime;
		private bool hasData = false;
		private string roomName;

		public SimNetworkEventStream(string roomName)
		{
			this.roomName = roomName;
			ClientId = ServiceManager.Resolve<PlatformService>().User.id.ToString();
			_syncFrames.Add(_nextFrame);
			Ready = true;
		}

		public List<SimFrame> Tick(long curFrame, long maxFrame, long expectedMaxFrame)
		{
			long now = GetTimeMs();
			if ((now - _lastReqTime) >= REQ_FREQ_MS)
			{
				_lastReqTime = now;
				var platform = ServiceManager.Resolve<PlatformService>();
				var req = new GameRelaySyncMsg();
				req.t = _nextFrame.Frame;
				for (int i = 0; i < _eventQueue.Count; i++)
				{
					var evt = new GameRelayEvent();
					evt.FromSimEvent(_eventQueue[i]);
					req.events.Add(evt);
				}

				_eventQueue.Clear();

				platform.GameRelay.Sync(roomName, req).Then(rsp =>
				{
					if (rsp.t == -1)
					{
						return;
					}

					// Purge all events we already might know about
					for (int j = 0; j < rsp.events.Count; j++)
					{
						var evt = rsp.events[j];
						if (evt.t > _nextFrame.Frame)
						{
							_nextFrame.Events.Add(evt.ToSimEvent());
						}
					}

					// If the response is higher than what we know, let's get it
					if (rsp.t > _nextFrame.Frame)
					{
						_nextFrame.Frame = rsp.t;
						hasData = true;
					}
				});
			}

			if (hasData)
			{
				hasData = false;
				return _syncFrames;
			}

			_nextFrame.Events.Clear();
			return _emptyFrames;
		}

		private long GetTimeMs()
		{
			return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		}

		public void SendEvent(SimEvent evt)
		{
			_eventQueue.Add(evt);
		}
	}
}
