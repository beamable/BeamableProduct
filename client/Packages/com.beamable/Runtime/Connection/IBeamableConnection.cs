using Beamable.Api;
using Beamable.Common;
using System;
using System.Collections.Generic;

namespace Connection
{
	public interface IBeamableConnection
	{
		event Action Open;
		event Action<string> Message;
		event Action<string> Error;
		event Action Close;
		
		Promise<Unit> Connect(string address, AccessToken token);
		Promise<Unit> Disconnect();
	}
}
