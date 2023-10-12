using Beamable.Common;
using System;
using UnityEngine;

namespace TimeoutCommon
{
	/*
	 * Code you intend to share between the Microservice and a Unity project
	 */
	public class Example
	{
		public static void Test()
		{
			var n = new Igloo();
			n.vec = new Vector2(3, 2);
		}
	}
}
