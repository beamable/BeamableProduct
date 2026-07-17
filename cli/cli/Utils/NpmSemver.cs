namespace cli.Utils;

/// <summary>
/// A minimal npm-flavored semver range matcher: just enough to answer "does this concrete version satisfy
/// this peerDependency range?" for the kinds of ranges that actually appear in package.json files — exact
/// pins (<c>1.2.3</c>), caret (<c>^1.2.3</c>), tilde (<c>~1.2</c>), x-ranges (<c>1.2.x</c>), comparators
/// (<c>&gt;=1.0.0 &lt;2.0.0</c>), hyphen ranges (<c>1.2.3 - 2.3.4</c>), and <c>||</c> unions.
///
/// It deliberately fails OPEN: if either the version or the range can't be fully parsed,
/// <see cref="TrySatisfies"/> returns <c>false</c> (undecided) so callers treat it as "no conflict" rather
/// than blocking a user on a range this matcher doesn't understand. Prerelease precedence is handled, but
/// npm's rule that prereleases only match comparators carrying a prerelease in the same tuple is NOT — that
/// also errs open (we may say "satisfied" where npm would not), which is the safe direction here.
/// </summary>
public static class NpmSemver
{
	private enum Op { Lt, Lte, Gt, Gte, Eq }

	/// <summary>
	/// Tries to decide whether <paramref name="version"/> satisfies <paramref name="range"/>.
	/// Returns true when a decision could be made (and writes it to <paramref name="satisfied"/>); returns
	/// false when the version or range couldn't be parsed, in which case <paramref name="satisfied"/> is false.
	/// </summary>
	public static bool TrySatisfies(string version, string range, out bool satisfied)
	{
		satisfied = false;
		if (range == null || !TryParseVersion(version, out var v))
		{
			return false;
		}

		// A range-set is one or more alternatives joined by "||"; the version matches if any alternative matches.
		var alternatives = range.Split(new[] { "||" }, System.StringSplitOptions.None);
		foreach (var alternative in alternatives)
		{
			if (!TryParseComparatorSet(alternative, out var comparators))
			{
				return false; // can't fully parse the range -> undecided
			}

			var matchesAll = true;
			foreach (var comparator in comparators)
			{
				if (!comparator.Matches(v))
				{
					matchesAll = false;
					break;
				}
			}

			if (matchesAll)
			{
				satisfied = true;
				return true;
			}
		}

		return true; // fully parsed, no alternative matched
	}

	private readonly struct Comparator
	{
		private readonly Op _op;
		private readonly SemVer _bound;

		public Comparator(Op op, SemVer bound)
		{
			_op = op;
			_bound = bound;
		}

		public bool Matches(SemVer v)
		{
			var cmp = SemVer.CompareFull(v, _bound);
			return _op switch
			{
				Op.Lt => cmp < 0,
				Op.Lte => cmp <= 0,
				Op.Gt => cmp > 0,
				Op.Gte => cmp >= 0,
				Op.Eq => cmp == 0,
				_ => false,
			};
		}
	}

	/// <summary>
	/// Parses one "||"-separated alternative — a whitespace-joined comparator set or a hyphen range — into the
	/// list of primitive comparators that must all hold.
	/// </summary>
	private static bool TryParseComparatorSet(string raw, out List<Comparator> comparators)
	{
		comparators = new List<Comparator>();
		var trimmed = raw.Trim();

		// Empty, "*", "x" => match anything.
		if (trimmed.Length == 0 || trimmed == "*" || trimmed == "x" || trimmed == "X")
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(0, 0, 0, null)));
			return true;
		}

		// Hyphen range: "A - B" (the spaces around the dash distinguish it from a prerelease dash).
		var hyphenParts = trimmed.Split(new[] { " - " }, System.StringSplitOptions.None);
		if (hyphenParts.Length == 2)
		{
			return TryExpandHyphen(hyphenParts[0].Trim(), hyphenParts[1].Trim(), comparators);
		}

		var tokens = trimmed.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
		foreach (var token in tokens)
		{
			if (!TryExpandToken(token, comparators))
			{
				return false;
			}
		}

		return comparators.Count > 0;
	}

	private static bool TryExpandHyphen(string low, string high, List<Comparator> comparators)
	{
		if (!TryParsePartial(low, out var lMajor, out var lMinor, out var lPatch, out var lPre))
		{
			return false;
		}

		if (!TryParsePartial(high, out var hMajor, out var hMinor, out var hPatch, out _))
		{
			return false;
		}

		// Lower bound: missing parts fill with zero.
		comparators.Add(new Comparator(Op.Gte, new SemVer(lMajor ?? 0, lMinor ?? 0, lPatch ?? 0, lPre)));

		// Upper bound: a partial high end rounds up to the next release boundary, otherwise it's inclusive.
		if (hMajor == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(0, 0, 0, null))); // "x" high end => unbounded above
		}
		else if (hMinor == null)
		{
			comparators.Add(new Comparator(Op.Lt, new SemVer(hMajor.Value + 1, 0, 0, null)));
		}
		else if (hPatch == null)
		{
			comparators.Add(new Comparator(Op.Lt, new SemVer(hMajor.Value, hMinor.Value + 1, 0, null)));
		}
		else
		{
			comparators.Add(new Comparator(Op.Lte, new SemVer(hMajor.Value, hMinor.Value, hPatch.Value, null)));
		}

		return true;
	}

	private static bool TryExpandToken(string token, List<Comparator> comparators)
	{
		var op = "";
		var rest = token;
		foreach (var candidate in new[] { ">=", "<=", "^", "~", ">", "<", "=" })
		{
			if (token.StartsWith(candidate))
			{
				op = candidate;
				rest = token.Substring(candidate.Length);
				break;
			}
		}

		if (!TryParsePartial(rest, out var major, out var minor, out var patch, out var pre))
		{
			return false;
		}

		switch (op)
		{
			case "^":
				return ExpandCaret(major, minor, patch, pre, comparators);
			case "~":
				return ExpandTilde(major, minor, patch, pre, comparators);
			case ">":
				return ExpandGreaterThan(major, minor, patch, pre, comparators);
			case ">=":
				comparators.Add(new Comparator(Op.Gte, new SemVer(major ?? 0, minor ?? 0, patch ?? 0, pre)));
				return true;
			case "<":
				return ExpandLessThan(major, minor, patch, pre, comparators);
			case "<=":
				return ExpandLessThanOrEqual(major, minor, patch, comparators);
			default: // "", "=" => exact when complete, x-range when partial
				return ExpandEquals(major, minor, patch, pre, comparators);
		}
	}

	private static bool ExpandCaret(int? major, int? minor, int? patch, string pre, List<Comparator> comparators)
	{
		if (major == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(0, 0, 0, null)));
			return true;
		}

		comparators.Add(new Comparator(Op.Gte, new SemVer(major.Value, minor ?? 0, patch ?? 0, pre)));

		SemVer high;
		if (minor == null)
		{
			high = new SemVer(major.Value + 1, 0, 0, null);
		}
		else if (patch == null)
		{
			high = major.Value != 0 ? new SemVer(major.Value + 1, 0, 0, null) : new SemVer(major.Value, minor.Value + 1, 0, null);
		}
		else if (major.Value != 0)
		{
			high = new SemVer(major.Value + 1, 0, 0, null);
		}
		else if (minor.Value != 0)
		{
			high = new SemVer(major.Value, minor.Value + 1, 0, null);
		}
		else
		{
			high = new SemVer(major.Value, minor.Value, patch.Value + 1, null);
		}

		comparators.Add(new Comparator(Op.Lt, high));
		return true;
	}

	private static bool ExpandTilde(int? major, int? minor, int? patch, string pre, List<Comparator> comparators)
	{
		if (major == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(0, 0, 0, null)));
			return true;
		}

		comparators.Add(new Comparator(Op.Gte, new SemVer(major.Value, minor ?? 0, patch ?? 0, pre)));
		var high = minor == null
			? new SemVer(major.Value + 1, 0, 0, null)
			: new SemVer(major.Value, minor.Value + 1, 0, null);
		comparators.Add(new Comparator(Op.Lt, high));
		return true;
	}

	private static bool ExpandGreaterThan(int? major, int? minor, int? patch, string pre, List<Comparator> comparators)
	{
		if (major == null)
		{
			// ">x" can never be satisfied; model as an unsatisfiable bound.
			comparators.Add(new Comparator(Op.Lt, new SemVer(0, 0, 0, null)));
			return true;
		}

		if (minor == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(major.Value + 1, 0, 0, null)));
		}
		else if (patch == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(major.Value, minor.Value + 1, 0, null)));
		}
		else
		{
			comparators.Add(new Comparator(Op.Gt, new SemVer(major.Value, minor.Value, patch.Value, pre)));
		}

		return true;
	}

	private static bool ExpandLessThan(int? major, int? minor, int? patch, string pre, List<Comparator> comparators)
	{
		if (major == null)
		{
			comparators.Add(new Comparator(Op.Lt, new SemVer(0, 0, 0, null)));
			return true;
		}

		var bound = new SemVer(major.Value, minor ?? 0, patch ?? 0, patch == null ? null : pre);
		comparators.Add(new Comparator(Op.Lt, bound));
		return true;
	}

	private static bool ExpandLessThanOrEqual(int? major, int? minor, int? patch, List<Comparator> comparators)
	{
		if (major == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(0, 0, 0, null)));
			return true;
		}

		if (minor == null)
		{
			comparators.Add(new Comparator(Op.Lt, new SemVer(major.Value + 1, 0, 0, null)));
		}
		else if (patch == null)
		{
			comparators.Add(new Comparator(Op.Lt, new SemVer(major.Value, minor.Value + 1, 0, null)));
		}
		else
		{
			comparators.Add(new Comparator(Op.Lte, new SemVer(major.Value, minor.Value, patch.Value, null)));
		}

		return true;
	}

	private static bool ExpandEquals(int? major, int? minor, int? patch, string pre, List<Comparator> comparators)
	{
		if (major == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(0, 0, 0, null)));
			return true;
		}

		if (minor == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(major.Value, 0, 0, null)));
			comparators.Add(new Comparator(Op.Lt, new SemVer(major.Value + 1, 0, 0, null)));
		}
		else if (patch == null)
		{
			comparators.Add(new Comparator(Op.Gte, new SemVer(major.Value, minor.Value, 0, null)));
			comparators.Add(new Comparator(Op.Lt, new SemVer(major.Value, minor.Value + 1, 0, null)));
		}
		else
		{
			comparators.Add(new Comparator(Op.Eq, new SemVer(major.Value, minor.Value, patch.Value, pre)));
		}

		return true;
	}

	/// <summary>
	/// Parses a concrete, complete version (major.minor.patch with an optional prerelease). Returns false for
	/// anything that isn't a full numeric version.
	/// </summary>
	private static bool TryParseVersion(string version, out SemVer result)
	{
		result = default;
		if (!TryParsePartial(version, out var major, out var minor, out var patch, out var pre))
		{
			return false;
		}

		if (major == null || minor == null || patch == null)
		{
			return false;
		}

		result = new SemVer(major.Value, minor.Value, patch.Value, pre);
		return true;
	}

	/// <summary>
	/// Parses a possibly-partial version such as "1", "1.2", "1.2.x", or "1.2.3-rc.1". Wildcards (x/X/*) and
	/// missing trailing parts come back as null. Build metadata (after '+') is ignored.
	/// </summary>
	private static bool TryParsePartial(string raw, out int? major, out int? minor, out int? patch, out string prerelease)
	{
		major = minor = patch = null;
		prerelease = null;

		if (raw == null)
		{
			return false;
		}

		var s = raw.Trim();
		if (s.StartsWith("v"))
		{
			s = s.Substring(1);
		}

		if (s.Length == 0 || s == "*" || s == "x" || s == "X")
		{
			return true; // all-wildcard
		}

		// Strip build metadata.
		var plus = s.IndexOf('+');
		if (plus >= 0)
		{
			s = s.Substring(0, plus);
		}

		// Split off the prerelease (first '-' after the numeric core).
		var dash = s.IndexOf('-');
		var core = s;
		if (dash >= 0)
		{
			core = s.Substring(0, dash);
			prerelease = s.Substring(dash + 1);
		}

		var parts = core.Split('.');
		if (parts.Length > 3)
		{
			return false;
		}

		var slots = new int?[3];
		var sawWildcard = false;
		for (var i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			if (part.Length == 0)
			{
				return false;
			}

			if (part == "x" || part == "X" || part == "*")
			{
				sawWildcard = true;
				slots[i] = null;
				continue;
			}

			if (sawWildcard || !int.TryParse(part, out var number) || number < 0)
			{
				// A concrete part following a wildcard is invalid, as is any non-numeric part.
				return false;
			}

			slots[i] = number;
		}

		major = slots[0];
		minor = slots[1];
		patch = slots[2];
		return true;
	}

	private readonly struct SemVer
	{
		public readonly int Major;
		public readonly int Minor;
		public readonly int Patch;
		public readonly string Prerelease;

		public SemVer(int major, int minor, int patch, string prerelease)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
			Prerelease = string.IsNullOrEmpty(prerelease) ? null : prerelease;
		}

		public static int CompareFull(SemVer a, SemVer b)
		{
			if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
			if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
			if (a.Patch != b.Patch) return a.Patch.CompareTo(b.Patch);

			// A version with a prerelease has lower precedence than the same version without one.
			if (a.Prerelease == null && b.Prerelease == null) return 0;
			if (a.Prerelease == null) return 1;
			if (b.Prerelease == null) return -1;
			return ComparePrerelease(a.Prerelease, b.Prerelease);
		}

		private static int ComparePrerelease(string a, string b)
		{
			var aIds = a.Split('.');
			var bIds = b.Split('.');
			var count = System.Math.Max(aIds.Length, bIds.Length);
			for (var i = 0; i < count; i++)
			{
				if (i >= aIds.Length) return -1; // a is a prefix of b => lower precedence
				if (i >= bIds.Length) return 1;

				var ai = aIds[i];
				var bi = bIds[i];
				var aNum = int.TryParse(ai, out var an);
				var bNum = int.TryParse(bi, out var bn);

				if (aNum && bNum)
				{
					if (an != bn) return an.CompareTo(bn);
				}
				else if (aNum) // numeric identifiers have lower precedence than alphanumeric
				{
					return -1;
				}
				else if (bNum)
				{
					return 1;
				}
				else
				{
					var cmp = string.CompareOrdinal(ai, bi);
					if (cmp != 0) return cmp < 0 ? -1 : 1;
				}
			}

			return 0;
		}
	}
}
