using Beamable.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Beamable.Editor
{
	public static class PromiseEditor
	{
		public static SequencePromise<T> ExecuteOnRoutines<T>(int routineCount,
															List<Func<Promise<T>>> generators)
		{
			return PromiseExtensions.ExecuteOnRoutines(
				routineCount: routineCount,
				coroutineExecutor: enumerator => EditorCoroutineUtility.StartCoroutineOwnerless(enumerator),
				generators: generators);
		}
	}
}
