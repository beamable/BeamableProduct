using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Coroutines
{
	public static class Yielders
	{
		static Dictionary<float, WaitForSeconds> _timeInterval = new Dictionary<float, WaitForSeconds>(20);

		static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();


		// XXX: Hey, are your tests not working in CI? This might be why. yield returning "null" is ever so slightly different than "endOfFrame"
		public static WaitForEndOfFrame EndOfFrame => Application.isBatchMode ? null : _endOfFrame;

		static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
		public static WaitForFixedUpdate FixedUpdate => _fixedUpdate;

		public static WaitForSeconds Seconds(float seconds)
		{
			if (!_timeInterval.ContainsKey(seconds))
				_timeInterval.Add(seconds, new WaitForSeconds(seconds));
			return _timeInterval[seconds];
		}
	}
}
