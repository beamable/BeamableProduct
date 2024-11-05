using Beamable.Common;
using System.Collections.Generic;

namespace Beamable.Api.Analytics
{
	public class LoginPanicEvent : CoreEvent
	{
		public LoginPanicEvent(string step, string error, bool retry)
		: base("loading", "login_panic", new Dictionary<string, object>
		{
			["step"] = step,
			["error"] = error,
			["retry"] = retry
		})
		{
			BeamableLogger.LogError($"LOGIN PANIC ({(retry ? "retry" : "terminal")}): {step} {error}");
		}
	}
}
