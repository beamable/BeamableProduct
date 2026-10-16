namespace Beamable.Api.Analytics.Batch
{
	/// <summary>Persistence for a batch manager. Implementations may buffer writes until Flush.</summary>
	public interface IAnalyticsBatchStorage
	{
		/// <summary>Returns false only for a missing key. Read failures must throw to prevent overwriting unreadable data.</summary>
		bool TryGetString(string key, out string value);
		/// <summary>Accepts a pending update. Persistence is confirmed only by Flush.</summary>
		bool SetString(string key, string value);
		/// <summary>Commits accepted updates. False leaves the batch eligible for retry.</summary>
		bool Flush();
	}
}
