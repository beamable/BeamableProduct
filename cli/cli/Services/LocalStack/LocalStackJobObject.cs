using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace cli.Services.LocalStack;

/// <summary>
/// A Windows Job Object configured with <c>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</c>: every process assigned to
/// it is killed by the OS the moment the last handle to the job closes — which happens automatically when the
/// owning process (this CLI) exits by <em>any</em> means (clean exit, Ctrl+C, terminal close, IDE stop, or a
/// hard <c>TerminateProcess</c>). Attached <c>beam local up</c> uses this so the whole stack — the
/// <c>cmd → powershell → java</c> Scala trees, <c>BeamableGateway.exe</c>, and the beam <c>Beamable.Tools</c>
/// microservice hosts — dies with the CLI instead of being orphaned. Job membership is inherited by child
/// processes, so assigning the launched wrapper covers its entire subtree.
///
/// No-op on non-Windows (job objects don't exist there); callers fall back to graceful teardown.
/// </summary>
public sealed class LocalStackJobObject : IDisposable
{
	private readonly SafeJobHandle _handle;

	private LocalStackJobObject(SafeJobHandle handle) => _handle = handle;

	/// <summary>Creates a kill-on-close job (or a no-op instance on non-Windows / on failure).</summary>
	public static LocalStackJobObject CreateKillOnClose()
	{
		if (!OperatingSystem.IsWindows())
			return new LocalStackJobObject(null);

		try { return new LocalStackJobObject(CreateKillOnCloseWindows()); }
		catch { return new LocalStackJobObject(null); }
	}

	/// <summary>Assigns a process (and, by inheritance, its whole future subtree) to the job.</summary>
	public void Assign(Process proc)
	{
		if (_handle == null || _handle.IsInvalid || proc == null || !OperatingSystem.IsWindows())
			return;

		try
		{
			// A process already in a job is fine on Win8+ (nested jobs); ignore the failure otherwise.
			AssignProcessToJobObject(_handle, proc.Handle);
		}
		catch { /* best-effort — graceful teardown remains the fallback */ }
	}

	public void Dispose() => _handle?.Dispose();

	// ---- Win32 interop -------------------------------------------------------------------------------

	[SupportedOSPlatform("windows")]
	private static SafeJobHandle CreateKillOnCloseWindows()
	{
		var handle = CreateJobObject(IntPtr.Zero, null);
		if (handle.IsInvalid)
			throw new InvalidOperationException("CreateJobObject failed");

		var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
			{
				LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
			}
		};

		var length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
		var ptr = Marshal.AllocHGlobal(length);
		try
		{
			Marshal.StructureToPtr(info, ptr, false);
			if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, ptr, (uint)length))
			{
				handle.Dispose();
				throw new InvalidOperationException("SetInformationJobObject failed");
			}
		}
		finally
		{
			Marshal.FreeHGlobal(ptr);
		}

		return handle;
	}

	private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
	private const int JobObjectExtendedLimitInformation = 9;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern SafeJobHandle CreateJobObject(IntPtr lpJobAttributes, string lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetInformationJobObject(SafeJobHandle hJob, int infoClass, IntPtr lpInfo, uint cbInfo);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AssignProcessToJobObject(SafeJobHandle hJob, IntPtr hProcess);

	private sealed class SafeJobHandle : SafeHandle
	{
		public SafeJobHandle() : base(IntPtr.Zero, ownsHandle: true) { }
		public override bool IsInvalid => handle == IntPtr.Zero;
		protected override bool ReleaseHandle() => CloseHandle(handle);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CloseHandle(IntPtr hObject);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public long PerProcessUserTimeLimit;
		public long PerJobUserTimeLimit;
		public uint LimitFlags;
		public UIntPtr MinimumWorkingSetSize;
		public UIntPtr MaximumWorkingSetSize;
		public uint ActiveProcessLimit;
		public UIntPtr Affinity;
		public uint PriorityClass;
		public uint SchedulingClass;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct IO_COUNTERS
	{
		public ulong ReadOperationCount;
		public ulong WriteOperationCount;
		public ulong OtherOperationCount;
		public ulong ReadTransferCount;
		public ulong WriteTransferCount;
		public ulong OtherTransferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
		public IO_COUNTERS IoInfo;
		public UIntPtr ProcessMemoryLimit;
		public UIntPtr JobMemoryLimit;
		public UIntPtr PeakProcessMemoryUsed;
		public UIntPtr PeakJobMemoryUsed;
	}
}
