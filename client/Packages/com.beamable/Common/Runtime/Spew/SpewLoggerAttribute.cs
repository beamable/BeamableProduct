// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using System;
using System.Diagnostics;

namespace Beamable.Common.Spew
{
	/// <summary>
	/// Conditional attribute to add Spew.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class SpewLoggerAttribute : Attribute { }
}
