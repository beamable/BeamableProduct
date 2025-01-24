﻿using System.CommandLine;

namespace cli.Options;

public class QuietOption : Option<bool>
{
	public QuietOption() : base("--quiet", () => false, "When true, skip input waiting and use default arguments (or error if no defaults are possible)")
	{
		AddAlias("-q");
	}
}
