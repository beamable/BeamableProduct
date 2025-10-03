using System.Diagnostics;
using System.Runtime.InteropServices;

public partial class CollectorManager
{
	
	private const int JobObjectExtendedLimitInformation = 9;
	
	// https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information
	private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
	
	// https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-createjobobjectw
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

	// https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-setinformationjobobject
	[DllImport("kernel32.dll")]
	private static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

	// https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-assignprocesstojobobject
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);
	
	private static bool StartProcessAttachedWindows(Process process)
	{
		bool started;
		var handle = CreateJobObject(IntPtr.Zero, null);

		var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE }
		};

		IntPtr extendedInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(info));
		Marshal.StructureToPtr(info, extendedInfoPtr, false);

		SetInformationJobObject(handle, JobObjectExtendedLimitInformation, extendedInfoPtr, (uint)Marshal.SizeOf(info));

		started = process.Start();
		AssignProcessToJobObject(handle, process.Handle);
		return started;
	}

	// Down here are some structure used by Windows kernel, we need them so we can assign our new process to a JobObject
	// This way, when the main process is killed, the JobObject will be killed too and with it the collector process will close too
	
	
	// https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information
	private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public long PerProcessUserTimeLimit;
		public long PerJobUserTimeLimit;
		public uint LimitFlags;
		public UIntPtr MinimumWorkingSetSize;
		public UIntPtr MaximumWorkingSetSize;
		public uint ActiveProcessLimit;
		public long Affinity;
		public uint PriorityClass;
		public uint SchedulingClass;
	}

	
	// https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information
	private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
		public IO_COUNTERS IoInfo;
		public UIntPtr ProcessMemoryLimit;
		public UIntPtr JobMemoryLimit;
		public UIntPtr PeakProcessMemoryUsed;
		public UIntPtr PeakJobMemoryUsed;
	}
	
	// https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-io_counters
	private struct IO_COUNTERS
	{
		public ulong ReadOperationCount;
		public ulong WriteOperationCount;
		public ulong OtherOperationCount;
		public ulong ReadTransferCount;
		public ulong WriteTransferCount;
		public ulong OtherTransferCount;
	}
}
