using Beamable.Common;
using Beamable.Coroutines;
using System;
using System.Collections;
using UnityEngine;

namespace Beamable
{
	public class BeamSystemGameObject : IGameObjectContext
	{
		private GameObject gob;

		public GameObject GameObject
		{
			get
			{
				if (!gob)
				{
					gob = new GameObject("Beamable System");
				}
				return gob;
			}
		}
	}


	public class BeamSystemScheduler : IScheduler
	{
		private readonly CoroutineService _coroutineService;

		public BeamSystemScheduler(CoroutineService coroutineService)
		{
			_coroutineService = coroutineService;
		}

		public Promise Delay()
		{
			var p = new Promise();

			IEnumerator Work()
			{
				yield return null;
				p.CompleteSuccess();
			}

			_coroutineService.StartNew("beamableDelay", Work());
			return p;
		}
	}
}
