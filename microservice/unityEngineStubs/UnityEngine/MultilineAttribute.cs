using System;
// ReSharper disable InconsistentNaming

namespace UnityEngine
{
    /// <summary>
    ///   <para>Attribute to make a string be edited with a multi-line textfield.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MultilineAttribute : PropertyAttribute
    {
        public readonly int lines;

        /// <summary>
        ///   <para>Attribute used to make a string value be shown in a multiline textarea.</para>
        /// </summary>
        public MultilineAttribute()
        {
            this.lines = 3;
        }

        /// <summary>
        ///   <para>Attribute used to make a string value be shown in a multiline textarea.</para>
        /// </summary>
        /// <param name="lines">How many lines of text to make room for. Default is 3.</param>
        public MultilineAttribute(int lines)
        {
            this.lines = lines;
        }
    }
}
