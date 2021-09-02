using System;
using System.Diagnostics;

namespace Beamable.Spew
{
   /// <summary>
   /// Conditional attribute to add Spew.
   /// </summary>
   [Conditional("UNITY_EDITOR")]
   [AttributeUsage(AttributeTargets.Class)]
   public sealed class SpewLoggerAttribute : Attribute {}
}
