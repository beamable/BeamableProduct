using System;
using UnityEngine;

namespace Beamable.Editor.Eggs
{
	[Serializable]
	public class EggEntry
	{
		private readonly KeyCode[] keySequence = new KeyCode[]
		{
			KeyCode.BackQuote, KeyCode.T, KeyCode.U, KeyCode.N, KeyCode.A,
		};

		public int sequenceIndex = 0;
		public bool entered = false;
		
		public void OnGui()
		{
			if (Event.current.type == EventType.KeyUp)
			{
				var code = Event.current.keyCode;
				if (sequenceIndex < keySequence.Length && code == keySequence[sequenceIndex])
				{
					sequenceIndex++;
				}
				else
				{
					sequenceIndex = 0;
				}

				if (sequenceIndex == keySequence.Length)
				{
					entered = true;
				}
			}
		}
	}
}
