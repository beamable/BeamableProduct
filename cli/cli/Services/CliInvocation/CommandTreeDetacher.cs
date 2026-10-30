using System.CommandLine;
using System.Reflection;

namespace cli.Services.CliInvocation;

/// <summary>
/// Unhooks a finished app's command tree from the options it shares with every other app.
/// <para>
/// Options such as <c>CidOption.Instance</c> or <c>QuietOption.Instance</c> are static singletons, and System.CommandLine
/// records every command they are added to as a parent. A process that builds an app per command (`beam server serve`,
/// the MCP server) would otherwise keep every finished command tree, with its reporter and services, reachable from
/// those statics, and each new app would walk an ever longer parent list.
/// </para>
/// </summary>
public static class CommandTreeDetacher
{
	/// <summary>
	/// Held while an app inside a long-lived process builds its command tree or detaches it, because
	/// System.CommandLine appends parents without synchronisation.
	/// </summary>
	public static readonly object TreeLock = new object();

	private static readonly FieldInfo FirstParentField =
		typeof(Symbol).GetField("_firstParent", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly Type ParentNodeType = typeof(Symbol).Assembly.GetType("System.CommandLine.ParentNode");
	private static readonly PropertyInfo NextProperty =
		ParentNodeType?.GetProperty("Next", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly PropertyInfo SymbolProperty =
		ParentNodeType?.GetProperty("Symbol", BindingFlags.Instance | BindingFlags.NonPublic);

	/// <summary>Whether this System.CommandLine version exposes the parent list this class edits.</summary>
	public static bool IsSupported => FirstParentField != null && NextProperty != null && SymbolProperty != null;

	public static void Detach(Command root)
	{
		if (root == null || !IsSupported)
		{
			return;
		}

		var commands = new HashSet<Symbol>(ReferenceEqualityComparer.Instance);
		var pending = new Stack<Command>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			var command = pending.Pop();
			if (!commands.Add(command))
			{
				continue;
			}
			foreach (var sub in command.Subcommands)
			{
				pending.Push(sub);
			}
		}

		lock (TreeLock)
		{
			foreach (Command command in commands)
			{
				foreach (var option in command.Options)
				{
					Unlink(option, commands);
				}
				foreach (var argument in command.Arguments)
				{
					Unlink(argument, commands);
				}
			}
		}
	}

	/// <summary>Removes the parents of <paramref name="symbol"/> that belong to the detached tree.</summary>
	private static void Unlink(Symbol symbol, HashSet<Symbol> tree)
	{
		object previous = null;
		var node = FirstParentField.GetValue(symbol);
		while (node != null)
		{
			var next = NextProperty.GetValue(node);
			if (tree.Contains((Symbol)SymbolProperty.GetValue(node)))
			{
				if (previous == null)
				{
					FirstParentField.SetValue(symbol, next);
				}
				else
				{
					NextProperty.SetValue(previous, next);
				}
			}
			else
			{
				previous = node;
			}
			node = next;
		}
	}
}
