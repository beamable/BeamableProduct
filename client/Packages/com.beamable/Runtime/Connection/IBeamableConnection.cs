using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Core.Platform.SDK;
using System;
using Beamable.Api.Sessions;
using Beamable.Common.Api;

namespace Beamable.Connection
{
	public interface IBeamableConnection 
		: IBeamableDisposable
		  
		  // the heartbeat service is a legacy concept where the sdk would
		  //  emit http calls every so often to say, "I'm alive!".
		  //  the websocket itself can do this better by noticing when the
		  //  connection drops/reconnects. 
		  // it made adoption easier to allow the new websocket approach
		  //  to mimmick the capabilities of the old heartbeat service. 
		, IHeartbeatService
	{
		event Action Open;
		event Action<string> Message;
		event Action<string> Error;
		event Action Close;

		Promise Connect(string address);
		Promise Disconnect();
	}
}
