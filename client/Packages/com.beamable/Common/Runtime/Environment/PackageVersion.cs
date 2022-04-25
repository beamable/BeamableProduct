using System;
using System.Text;
using UnityEngine;

namespace Beamable.Common
{
	[Serializable]
	public class PackageVersion
	{
		private const string PREVIEW_STRING = "PREVIEW";
		private const string RC_STRING = "RC";
		private const string NIGHTLY_STRING = "NIGHTLY";
		private const int UNASSIGNED_VALUE = -1;

		private const char VERSION_SEPARATOR = '.';
		private const char PREVIEW_SEPARATOR = '-';

		[SerializeField] private int _major = UNASSIGNED_VALUE;
		[SerializeField] private int _minor = UNASSIGNED_VALUE;
		[SerializeField] private int _patch = UNASSIGNED_VALUE;
		[SerializeField] private int _rc = UNASSIGNED_VALUE;
		[SerializeField] private long _nightlyTime = UNASSIGNED_VALUE;
		[SerializeField] private bool _isPreview;

		public bool IsReleaseCandidate => _rc > UNASSIGNED_VALUE;
		public bool IsNightly => _nightlyTime > UNASSIGNED_VALUE;
		public bool IsPreview => _isPreview;

		public int Major => _major;
		public int Minor => _minor;
		public int Patch => _patch;
		public long? NightlyTime => IsNightly ? _nightlyTime : default;
		public int? RC => IsReleaseCandidate ? _rc : default;

		public PackageVersion(int major, int minor, int patch, int rc = -1, long nightlyTime = -1, bool isPreview = false)
		{
			_major = major;
			_minor = minor;
			_patch = patch;
			_rc = rc;
			_nightlyTime = nightlyTime;
			_isPreview = isPreview;
		}


		protected bool Equals(PackageVersion other)
		{
			return _major == other._major && _minor == other._minor && _patch == other._patch && _rc == other._rc && _nightlyTime == other._nightlyTime && _isPreview == other._isPreview;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((PackageVersion)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = _major;
				hashCode = (hashCode * 397) ^ _minor;
				hashCode = (hashCode * 397) ^ _patch;
				hashCode = (hashCode * 397) ^ _rc;
				hashCode = (hashCode * 397) ^ _nightlyTime.GetHashCode();
				hashCode = (hashCode * 397) ^ _isPreview.GetHashCode();
				return hashCode;
			}
		}

		public bool IsMinor(int major, int minor) => IsMajor(major) && Minor == minor;

		public bool IsMajor(int major) => Major == major;

		public static bool operator <(PackageVersion a, PackageVersion b)
		{
			return a.Major < b.Major || (a.Major <= b.Major && a.Minor < b.Minor) || (a.Major <= b.Minor && a.Minor <= b.Minor && (a.Patch < b.Patch));
		}

		public static bool operator >(PackageVersion b, PackageVersion a)
		{
			return a.Major < b.Major || (a.Major <= b.Major && a.Minor < b.Minor) || (a.Major <= b.Minor && a.Minor <= b.Minor && a.Patch < b.Patch);
		}

		public static bool operator ==(PackageVersion a, PackageVersion b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(PackageVersion a, PackageVersion b)
		{
			return !(a == b);
		}

		public static bool operator <=(PackageVersion a, PackageVersion b)
		{
			return a < b || a == b;
		}
		public static bool operator >=(PackageVersion a, PackageVersion b)
		{
			return a > b || a == b;
		}

		public static implicit operator PackageVersion(string versionString) => PackageVersion.FromSemanticVersionString(versionString);


		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(_major);
			sb.Append(VERSION_SEPARATOR);
			sb.Append(_minor);
			sb.Append(VERSION_SEPARATOR);
			sb.Append(_patch);
			if (_isPreview)
			{
				sb.Append(PREVIEW_SEPARATOR);
				sb.Append(PREVIEW_STRING);
			}

			if (IsNightly)
			{
				sb.Append(VERSION_SEPARATOR);
				sb.Append(NIGHTLY_STRING);
				sb.Append(PREVIEW_SEPARATOR);
				sb.Append(_nightlyTime);
			}

			if (IsReleaseCandidate)
			{
				sb.Append(VERSION_SEPARATOR);
				sb.Append(RC_STRING);
				sb.Append(_rc);
			}

			return sb.ToString();
		}

		public static bool TryFromSemanticVersionString(string semanticVersion, out PackageVersion version)
		{
			try
			{
				version = semanticVersion;
				return true;
			}
			catch
			{
				version = new PackageVersion(0, 0, 0);
				return false;
			}
		}
		public static PackageVersion FromSemanticVersionString(string semanticVersion)
		{
			var major = -1;
			var minor = -1;
			var patch = -1;
			var rc = -1;
			var nightlyTime = -1L;
			var isPreview = false;

			var buffer = "";
			for (var i = 0; i < semanticVersion.Length; i++)
			{
				var c = semanticVersion[i];
				if (!isPreview && buffer.Equals(PREVIEW_STRING))
				{
					isPreview = true;
					buffer = "";
				}

				if (buffer.Equals(RC_STRING))
				{
					if (!int.TryParse(semanticVersion.Substring(i, semanticVersion.Length - i), out rc))
					{
						throw new ArgumentException("rc version not an int");
					}
					break;
				}

				if (buffer.Equals(NIGHTLY_STRING))
				{
					// add one to ignore the expected - character
					if (!long.TryParse(semanticVersion.Substring(i + 1, semanticVersion.Length - (i + 1)), out nightlyTime))
					{
						throw new ArgumentException("nightly time not a long");
					}

					break;
				}

				switch (c)
				{
					case VERSION_SEPARATOR when major == UNASSIGNED_VALUE:
						if (!int.TryParse(buffer, out major))
						{
							throw new ArgumentException("Major version not an int");
						}

						buffer = "";
						break;
					case VERSION_SEPARATOR when minor == UNASSIGNED_VALUE:
						if (!int.TryParse(buffer, out minor))
						{
							throw new ArgumentException("Minor version not an int");
						}

						buffer = "";
						break;
					case PREVIEW_SEPARATOR when patch == UNASSIGNED_VALUE:
						if (!int.TryParse(buffer, out patch))
						{
							throw new ArgumentException("Patch version not an int");
						}

						buffer = "";
						break;
					case PREVIEW_SEPARATOR:
					case VERSION_SEPARATOR:
						break;
					default:
						buffer += c;
						break;
				}

				var lastChar = i == semanticVersion.Length - 1;
				if (lastChar && patch == UNASSIGNED_VALUE)
				{
					if (!int.TryParse(buffer, out patch))
					{
						throw new ArgumentException("Patch version not an int");
					}
				}

			}

			return new PackageVersion(
			   major: major,
			   minor: minor,
			   patch: patch,
			   rc: rc,
			   nightlyTime: nightlyTime,
			   isPreview: isPreview);
		}
	}
}
