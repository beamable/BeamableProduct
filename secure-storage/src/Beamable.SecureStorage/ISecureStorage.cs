using System;

namespace Beamable.Security
{
	/// <summary>Buffered string storage. Call Flush explicitly to persist changes.</summary>
	public interface ISecureStorage : IDisposable
	{
		/// <summary>Accepts an in-memory update; true does not mean the value is on disk yet.</summary>
		bool SetString(string key, string value);

		/// <summary>Reads the latest value, including pending changes. Missing keys return false and null.</summary>
		bool TryGetString(string key, out string? value);

		/// <summary>Accepts a pending deletion. An already absent key also returns true.</summary>
		bool DeleteKey(string key);

		/// <summary>Persists pending changes. On failure, changes remain pending for retry.</summary>
		bool Flush();
	}
}
