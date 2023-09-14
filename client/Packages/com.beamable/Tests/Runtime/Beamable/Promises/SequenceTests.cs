using Beamable.Common;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace Beamable.Tests.Runtime.PromiseTests
{
	public class SequenceTests
	{


		[UnityTest]
		public IEnumerator Sequence_Order()
		{
			var p0 = new Promise<int>();
			var p1 = new Promise<int>();
			var p2 = new Promise<int>();
			var promises = new List<Promise<int>>
			{
				p0, p1, p2
			};

			var sequence = Promise.Sequence(promises);

			// complete the promises out of order.
			yield return null;
			p1.CompleteSuccess(5);

			yield return null;
			p2.CompleteSuccess(7);

			yield return null;
			p0.CompleteSuccess(11);

			yield return null;
			yield return sequence.ToYielder();

			var result = sequence.GetResult();
			Assert.AreEqual(11, result[0]); // p0
			Assert.AreEqual(5, result[1]);  // p1
			Assert.AreEqual(7, result[2]);  // p2
		}
	}
}
