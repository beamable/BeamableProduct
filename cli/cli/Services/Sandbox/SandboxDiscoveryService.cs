using System;
using System.Linq;
using Beamable.Server;

namespace cli.Services.Sandbox;

/// <summary>
/// The sandbox <see cref="Microservice"/> route surface. All business logic lives in
/// <see cref="SandboxObserver"/>; this class is the thin façade that pulls
/// <see cref="RequestContext.AccountId"/> off the gateway-authenticated context and
/// forwards to the observer.
///
/// Phase 1 ships <c>Pair</c>, <c>Info</c>, and <c>Shutdown</c>. HMAC enforcement on
/// non-pair routes lands with Phase 2 (Invoke + file operations).
/// </summary>
public class SandboxDiscoveryService : Microservice
{
	[ClientCallable]
	public SandboxPairResponse Pair(string joinCode)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.Pair(Context.AccountId, joinCode);

		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "pair-failed", "Pair failed");
		}

		var session = outcome.Session!;
		return new SandboxPairResponse
		{
			sessionId = session.Id,
			hmacSecretBase64 = Convert.ToBase64String(session.HmacSecret),
			sandboxInfo = BuildInfoDto(observer),
		};
	}

	[ClientCallable]
	public SandboxInfoResponse Info()
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.GetInfo(Context.AccountId);

		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "info-failed", "Info failed");
		}

		return BuildInfoDto(observer);
	}

	/// <summary>
	/// Returns the CLI command schema this sandbox can execute. Portal diffs this
	/// against its own bundled schema at pair-time to figure out which commands
	/// are safely callable across version-skew. Computed once at sandbox startup
	/// and served from cache.
	/// </summary>
	[ClientCallable]
	public SandboxGetSchemaResponse GetSchema()
	{
		var observer = Provider.GetService<SandboxObserver>();
		var schema = observer.Schema;
		if (schema == null)
		{
			throw new MicroserviceException(503, "schema-unavailable",
				"Sandbox is still starting; schema not yet bound");
		}
		return new SandboxGetSchemaResponse
		{
			cliVersion = schema.cliVersion,
			schema = schema,
		};
	}

	[ClientCallable]
	public SandboxShutdownResponse Shutdown()
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.Shutdown(Context.AccountId, ShutdownReason.UserRequested);

		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "shutdown-failed", "Shutdown failed");
		}

		// The MS host awaits ShutdownSignal and returns once it fires. The schedule below
		// gives the response a moment to flush back to the gateway before the host loop
		// completes and the process exits.
		return new SandboxShutdownResponse { ok = true };
	}

	[ClientCallable]
	public SandboxInvokeResponse Invoke(string commandLine, bool outOfProcess = false)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.Invoke(Context.AccountId, commandLine, outOfProcess);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "invoke-failed", "Invoke failed");
		}
		return new SandboxInvokeResponse { invocationId = outcome.InvocationId! };
	}

	[ClientCallable]
	public SandboxListInvocationsResponse ListInvocations()
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.ListInvocations(Context.AccountId);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "list-failed", "ListInvocations failed");
		}
		return new SandboxListInvocationsResponse
		{
			items = outcome.Items!.Select(ToDto).ToArray(),
		};
	}

	[ClientCallable]
	public SandboxGetOutputResponse GetInvocationOutput(string invocationId, long sinceSequence = -1)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.GetInvocationOutput(
			Context.AccountId,
			invocationId,
			sinceSequence < 0 ? null : sinceSequence);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "get-output-failed", "GetInvocationOutput failed");
		}
		var page = outcome.Page!;
		return new SandboxGetOutputResponse
		{
			invocationStatus = outcome.InvocationStatus!,
			exitCode = outcome.ExitCode,
			droppedCount = page.DroppedCount,
			nextSequence = page.NextSequence,
			lines = page.Lines.Select(l => new SandboxOutputLineDto
			{
				sequence = l.Sequence,
				channel = l.Channel,
				line = l.Line,
			}).ToArray(),
		};
	}

	[ClientCallable]
	public SandboxCancelResponse CancelInvocation(string invocationId)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.CancelInvocation(Context.AccountId, invocationId);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "cancel-failed", "CancelInvocation failed");
		}
		return new SandboxCancelResponse { ok = true };
	}

	[ClientCallable]
	public SandboxListDirResponse ListDir(string path, bool showHidden = false)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.ListDir(Context.AccountId, path, showHidden);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "listdir-failed", "ListDir failed");
		}
		var entries = outcome.Listing!.Entries.Select(e => new SandboxDirEntryDto
		{
			name = e.Name, kind = e.Kind, size = e.Size, modifiedAtUnixMs = e.ModifiedAtUnixMs,
		}).ToArray();
		return new SandboxListDirResponse { entries = entries };
	}

	[ClientCallable]
	public SandboxStatResponse Stat(string path)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.Stat(Context.AccountId, path);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "stat-failed", "Stat failed");
		}
		var s = outcome.Stat!;
		return new SandboxStatResponse
		{
			kind = s.Kind, size = s.Size, modifiedAtUnixMs = s.ModifiedAtUnixMs, contentHash = s.ContentHash,
		};
	}

	[ClientCallable]
	public SandboxReadFileResponse ReadFile(string path, long rangeStart = -1, long rangeEnd = -1)
	{
		var observer = Provider.GetService<SandboxObserver>();
		long? start = rangeStart < 0 ? null : rangeStart;
		long? end = rangeEnd < 0 ? null : rangeEnd;
		var outcome = observer.ReadFile(Context.AccountId, path, start, end);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "read-failed", "ReadFile failed");
		}
		var r = outcome.Result!;
		return new SandboxReadFileResponse
		{
			contents = r.Contents, size = r.Size, modifiedAtUnixMs = r.ModifiedAtUnixMs, contentHash = r.ContentHash,
		};
	}

	[ClientCallable]
	public SandboxWriteFileResponse WriteFile(string path, string contents, string expectedContentHash = "")
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.WriteFile(
			Context.AccountId,
			path,
			contents,
			string.IsNullOrEmpty(expectedContentHash) ? null : expectedContentHash);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			// 409 conflict carries the current hash so the client can fetch + merge.
			if ((int)outcome.Status == 409)
			{
				throw new MicroserviceException(409, "conflict", outcome.ConflictHash ?? "");
			}
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "write-failed", "WriteFile failed");
		}
		var w = outcome.Result!;
		return new SandboxWriteFileResponse
		{
			size = w.Size, modifiedAtUnixMs = w.ModifiedAtUnixMs, contentHash = w.ContentHash,
		};
	}

	[ClientCallable]
	public SandboxOkResponse DeleteFile(string path)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.DeleteFile(Context.AccountId, path);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "delete-failed", "DeleteFile failed");
		}
		return new SandboxOkResponse { ok = true };
	}

	[ClientCallable]
	public SandboxOkResponse Rename(string fromPath, string toPath)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.Rename(Context.AccountId, fromPath, toPath);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "rename-failed", "Rename failed");
		}
		return new SandboxOkResponse { ok = true };
	}

	[ClientCallable]
	public SandboxOkResponse MakeDir(string path)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.MakeDir(Context.AccountId, path);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "mkdir-failed", "MakeDir failed");
		}
		return new SandboxOkResponse { ok = true };
	}

	[ClientCallable]
	public SandboxWatchPathsResponse WatchPaths(string[] paths)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.WatchPaths(Context.AccountId, paths);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "watch-failed", "WatchPaths failed");
		}
		return new SandboxWatchPathsResponse { watchId = outcome.WatchId! };
	}

	[ClientCallable]
	public SandboxOkResponse UnwatchPaths(string watchId)
	{
		var observer = Provider.GetService<SandboxObserver>();
		var outcome = observer.UnwatchPaths(Context.AccountId, watchId);
		if (outcome.Status != SandboxRouteStatus.Ok)
		{
			throw new MicroserviceException((int)outcome.Status, outcome.Error ?? "unwatch-failed", "UnwatchPaths failed");
		}
		return new SandboxOkResponse { ok = true };
	}

	private static SandboxInvocationDto ToDto(InvocationSummary s) => new()
	{
		invocationId = s.Id,
		commandLine = s.CommandLine,
		status = s.Status,
		exitCode = s.ExitCode,
		startedAtUnixMs = s.StartedAtUnixMs,
		lineCount = s.LineCount,
		nextSequence = s.NextSequence,
	};

	private static SandboxInfoResponse BuildInfoDto(SandboxObserver observer)
	{
		var snap = observer.GetInfo(observer.LauncherAccountId).Snapshot!;
		return new SandboxInfoResponse
		{
			name = snap.Name,
			label = snap.Label,
			repoRoot = snap.RepoRoot,
			cid = snap.Cid,
			pid = snap.Pid,
			startedAtUnixMs = snap.StartedAt.ToUnixTimeMilliseconds(),
			sandboxVersion = snap.SandboxVersion,
			activeSessionCount = snap.ActiveSessionCount,
			connectionHistory = snap.ConnectionHistory.Select(e => new SandboxConnectionEventDto
			{
				kind = e.Kind.ToString().ToLowerInvariant(),
				sessionId = e.SessionId,
				atUnixMs = e.At.ToUnixTimeMilliseconds(),
			}).ToArray(),
		};
	}
}

[Serializable]
public class SandboxPairResponse
{
	public string sessionId = string.Empty;
	public string hmacSecretBase64 = string.Empty;
	public SandboxInfoResponse sandboxInfo = new();
}

[Serializable]
public class SandboxInfoResponse
{
	public string name = string.Empty;
	public string? label;
	public string repoRoot = string.Empty;
	public string cid = string.Empty;
	public string pid = string.Empty;
	public long startedAtUnixMs;
	public string sandboxVersion = string.Empty;
	public int activeSessionCount;
	public SandboxConnectionEventDto[] connectionHistory = Array.Empty<SandboxConnectionEventDto>();
}

[Serializable]
public class SandboxConnectionEventDto
{
	public string kind = string.Empty;
	public string? sessionId;
	public long atUnixMs;
}

[Serializable]
public class SandboxShutdownResponse
{
	public bool ok;
}

[Serializable]
public class SandboxGetSchemaResponse
{
	public string cliVersion = string.Empty;
	public CliSchema schema = new();
}

[Serializable]
public class SandboxInvokeResponse
{
	public string invocationId = string.Empty;
}

[Serializable]
public class SandboxListInvocationsResponse
{
	public SandboxInvocationDto[] items = Array.Empty<SandboxInvocationDto>();
}

[Serializable]
public class SandboxInvocationDto
{
	public string invocationId = string.Empty;
	public string commandLine = string.Empty;
	public string status = string.Empty;
	public int? exitCode;
	public long startedAtUnixMs;
	public int lineCount;
	public long nextSequence;
}

[Serializable]
public class SandboxGetOutputResponse
{
	public string invocationStatus = string.Empty;
	public int? exitCode;
	public long droppedCount;
	public long nextSequence;
	public SandboxOutputLineDto[] lines = Array.Empty<SandboxOutputLineDto>();
}

[Serializable]
public class SandboxOutputLineDto
{
	public long sequence;
	public string channel = string.Empty;
	public string line = string.Empty;
}

[Serializable]
public class SandboxCancelResponse
{
	public bool ok;
}

[Serializable]
public class SandboxOkResponse
{
	public bool ok;
}

[Serializable]
public class SandboxListDirResponse
{
	public SandboxDirEntryDto[] entries = Array.Empty<SandboxDirEntryDto>();
}

[Serializable]
public class SandboxDirEntryDto
{
	public string name = string.Empty;
	public string kind = "file";
	public long size;
	public long modifiedAtUnixMs;
}

[Serializable]
public class SandboxStatResponse
{
	public string kind = "file";
	public long size;
	public long modifiedAtUnixMs;
	public string? contentHash;
}

[Serializable]
public class SandboxReadFileResponse
{
	public string contents = string.Empty;
	public long size;
	public long modifiedAtUnixMs;
	public string? contentHash;
}

[Serializable]
public class SandboxWriteFileResponse
{
	public long size;
	public long modifiedAtUnixMs;
	public string? contentHash;
}

[Serializable]
public class SandboxWatchPathsResponse
{
	public string watchId = string.Empty;
}
