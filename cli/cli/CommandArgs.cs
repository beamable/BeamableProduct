using System.CommandLine;
using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;

namespace cli;

public abstract class CommandArgs
{
	public bool Dryrun { get; set; }
}
