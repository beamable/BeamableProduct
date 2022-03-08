using System;

namespace Beamable.Common.Api
{
	public static class AliasHelper
	{
		public static bool IsCid(string cid)
		{
			// a cid must start with a number.
			if (string.IsNullOrEmpty(cid)) return false;
			return char.IsDigit(cid[0]);
		}

		public static void ValidateAlias(string alias)
		{
			if (string.IsNullOrWhiteSpace(alias)) return;
			if (IsCid(alias)) throw new ArgumentException(nameof(alias) + " is a cid");
		}

		public static void ValidateCid(string cid)
		{
			if (string.IsNullOrWhiteSpace(cid)) return;
			if (!IsCid(cid)) throw new ArgumentException(nameof(cid) + " is not a cid");
		}
	}
}
