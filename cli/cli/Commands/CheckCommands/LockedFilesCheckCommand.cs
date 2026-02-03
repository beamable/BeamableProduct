using Newtonsoft.Json;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cli.CheckCommands;

[Serializable]
public class LockedFilesCheckCommandArgs : CommandArgs
{
	public string folderPath;
	public string filePattern;
}

[Serializable]
public class LockedFilesCheckCommandResult
{
	public List<ProcessInfo> ProcessLockedFiles;
}

[Serializable]
public class ProcessInfo
{
	public int ProcessId;
	public string CommandLine;
	public List<string> LockingFiles;
}

public class LockedFilesCheckCommand : StreamCommand<LockedFilesCheckCommandArgs, LockedFilesCheckCommandResult>
{
	public LockedFilesCheckCommand() : base("locked-files", "Check if there are any locked file inside a folder")
	{
	}

	public override void Configure()
	{
		var folderPath = new Argument<string>(nameof(LockedFilesCheckCommandArgs.folderPath))
		{
			Description = "The folder path to check for locked files"
		};

		AddArgument(folderPath, (args, i) => args.folderPath = i);
		AddOption(
			new Option<string>($"--pattern", () => "*",
				"The file pattern to check for"), (args, i) => args.filePattern = i);
	}

	public override Task Handle(LockedFilesCheckCommandArgs args)
	{
	    if (string.IsNullOrWhiteSpace(args.folderPath))
	    {
		    throw new CliException($"Missing value on argument: {args.folderPath}");
	    }
	
	    if (!Directory.Exists(args.folderPath))
	    {
		    throw new CliException($"Not directory found for path: {args.folderPath}");
	    }

	    var processLockedFiles = new Dictionary<int, List<string>>();
	
	    foreach (string file in Directory.EnumerateFiles(args.folderPath, args.filePattern, SearchOption.AllDirectories))
	    {
	        try
	        {
		        using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
		        { 
			        // File is not locked
		        }
	        }
	        catch (UnauthorizedAccessException)
	        {
	            // skip inaccessible files
	        }
	        catch (IOException)
	        {
		        {
			        List<Process> lockingProcesses = GetLockingProcesses(file);
			        foreach (Process process in lockingProcesses)
			        {
				        if (!processLockedFiles.TryGetValue(process.Id, out List<string> value))
				        {
					        value = new List<string>();
					        processLockedFiles[process.Id] = value;
				        }

				        value.Add(file);
			        }
		        }
	        }
	    }
	    
	    var result = new LockedFilesCheckCommandResult
	    {
		    ProcessLockedFiles = processLockedFiles.Select(kv => new ProcessInfo
		    {
			    ProcessId = kv.Key,
			    CommandLine = GetProcessCommandLine(kv.Key),
			    LockingFiles = kv.Value
		    }).ToList()
	    };
	    
	    SendResults(result);
	    Console.Write(JsonConvert.SerializeObject(result, Formatting.Indented));
	    return Task.CompletedTask;
	}
	
	private static List<Process> GetLockingProcesses(string filePath)
    {
        var processes = new List<Process>();
        
        uint sessionHandle;
        string sessionKey = Guid.NewGuid().ToString();
        
        if (RmStartSession(out sessionHandle, 0, sessionKey) != 0)
            return processes;

        try
        {
            string[] resources = { filePath };
            if (RmRegisterResources(sessionHandle, (uint)resources.Length, resources, 0, null, 0, null) != 0)
                return processes;

            uint pnProcInfoNeeded = 0, pnProcInfo = 0, lpdwRebootReasons = 0;
            int res = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

            if (pnProcInfoNeeded > 0)
            {
                var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                pnProcInfo = pnProcInfoNeeded;

                if (RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons) == 0)
                {
                    for (int i = 0; i < pnProcInfo; i++)
                    {
                        try
                        {
                            processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                        }
                        catch { }
                    }
                }
            }
        }
        finally
        {
            RmEndSession(sessionHandle);
        }

        return processes;
    }

	private static string GetProcessCommandLine(int processId)
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				using var searcher = new System.Management.ManagementObjectSearcher(
					$"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
				foreach (var obj in searcher.Get())
				{
					return obj["CommandLine"]?.ToString() ?? string.Empty;
				}
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
			         RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{

				string cmdlinePath = $"/proc/{processId}/cmdline";
				if (File.Exists(cmdlinePath))
				{
					string cmdline = File.ReadAllText(cmdlinePath).Replace('\0', ' ').Trim();
					return cmdline;
				}
			}

		}
		catch
		{
			return string.Empty;
		}

		return string.Empty;
	}

	[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames, uint nApplications, RM_UNIQUE_PROCESS[] rgApplications, uint nServices, string[] rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, ref uint lpdwRebootReasons);

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string strServiceShortName;
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }
}
