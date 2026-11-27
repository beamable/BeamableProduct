namespace cli.Content;

/// <summary>Orders publication delivery against the guarded watcher's first snapshot.</summary>
internal sealed class LocalModeExitActivation
{
	private readonly object _gate = new();
	private readonly HashSet<string> _observed = new(StringComparer.Ordinal);
	private string _activatedUid;
	private bool _disconnected;
	internal TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

	internal bool ShouldProcessPublication(string uid)
	{
		lock (_gate)
		{
			if (_activatedUid != null)
				return _activatedUid != uid;

			_observed.Add(uid);
			return false;
		}
	}

	internal void Activate(string checkedUid, CancellationToken token, Action report)
	{
		lock (_gate)
		{
			token.ThrowIfCancellationRequested();
			if (_disconnected)
				throw Disconnected();
			if (_observed.Any(uid => uid != checkedUid))
				throw RealmChanged();

			// The consumer must see the confirmed snapshot before later publications may start syncing.
			report();
			_activatedUid = checkedUid;
		}
	}

	internal void OnReconnecting()
	{
		lock (_gate)
		{
			if (_activatedUid == null)
			{
				_disconnected = true;
				throw Disconnected();
			}
		}
	}

	internal void OnDisconnected()
	{
		lock (_gate)
		{
			_disconnected = true;
		}
	}

	private static CliException Disconnected() =>
		new("The publication watcher disconnected during the Local Mode exit check. Refresh and try again.");

	internal static CliException RealmChanged() =>
		new("The realm changed during the Local Mode exit check. Refresh and review before trying again.");
}
