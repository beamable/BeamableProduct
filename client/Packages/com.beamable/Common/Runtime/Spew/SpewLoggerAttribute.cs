// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC4

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
