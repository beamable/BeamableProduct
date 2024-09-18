using System;
// ReSharper disable InconsistentNaming

namespace UnityEngine
{
  /// <summary>
  ///   <para>The ContextMenu attribute allows you to add commands to the context menu.</para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
  public sealed class ContextMenu : Attribute
  {
    public readonly string menuItem;
    public readonly bool validate;
    public readonly int priority;

    /// <summary>
    ///   <para>Adds the function to the context menu of the component.</para>
    /// </summary>
    /// <param name="itemName">The name of the context menu item.</param>
    public ContextMenu(string itemName)
      : this(itemName, false)
    {
    }

    /// <summary>
    ///   <para>Adds the function to the context menu of the component.</para>
    /// </summary>
    /// <param name="itemName">The name of the context menu item.</param>
    /// <param name="isValidateFunction">Whether this is a validate function (defaults to false).</param>
    public ContextMenu(string itemName, bool isValidateFunction)
      : this(itemName, isValidateFunction, 1000000)
    {
    }

    /// <summary>
    ///   <para>Adds the function to the context menu of the component.</para>
    /// </summary>
    /// <param name="itemName">The name of the context menu item.</param>
    /// <param name="isValidateFunction">Whether this is a validate function (defaults to false).</param>
    /// <param name="priority">Priority used to override the ordering of the menu items (defaults to 1000000). The lower the number the earlier in the menu it will appear.</param>
    public ContextMenu(string itemName, bool isValidateFunction, int priority)
    {
      this.menuItem = itemName;
      this.validate = isValidateFunction;
      this.priority = priority;
    }
  }
}
