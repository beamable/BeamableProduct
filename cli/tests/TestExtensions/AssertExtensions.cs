using cli.Utils;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace tests;

public static class BIs
{
	public static EqualConstraint Path(string path)
	{
		return Is.EqualTo(path.LocalizeSlashes());
	}
}
