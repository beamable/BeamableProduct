
using UnityEngine;

namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	[System.SerializableAttribute()]
	public class BeamTailLogMessage
	{
		[SerializeField] string __t;
		[SerializeField] string __m;
		[SerializeField] string __l;
		[SerializeField] string __raw;
		public string timeStamp => __t;
		public string message => __m;
		public string logLevel => __l;
		public string raw => __raw;
	}
}
