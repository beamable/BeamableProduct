using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Core.Platform.SDK;
using System;

namespace Beamable.Connection
{
	public interface IBeamableConnection : IBeamableDisposable
	{
		event Action Open;
		event Action<string> Message;
		event Action<string> Error;
		event Action Close;

		Promise Connect(string address, IBeamableApiRequester token);
		Promise Disconnect();
	}
}
