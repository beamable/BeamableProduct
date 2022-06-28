using System;
using System.IO;

namespace Beamable.Server
{
	public interface IMicroserviceArgs : IRealmInfo
	{
		string Host { get; }
		string Secret { get; }
		string NamePrefix { get; }
		string SdkVersionBaseBuild { get; }
		string SdkVersionExecution { get; }
		bool WatchToken { get; }
	}

	public class MicroserviceArgs : IMicroserviceArgs
	{
		public string CustomerID { get; set; }
		public string ProjectName { get; set; }
		public string Secret { get; set; }
		public string Host { get; set; }
		public string NamePrefix { get; set; }
		public string SdkVersionBaseBuild { get; set; }
		public string SdkVersionExecution { get; set; }
		public bool WatchToken { get; set; }
	}

	public static class MicroserviceArgsExtensions
	{
		public static IMicroserviceArgs Copy(this IMicroserviceArgs args)
		{
			return new MicroserviceArgs
			{
				CustomerID = args.CustomerID,
				ProjectName = args.ProjectName,
				Secret = args.Secret,
				Host = args.Host,
				NamePrefix = args.NamePrefix,
				SdkVersionBaseBuild = args.SdkVersionBaseBuild,
				SdkVersionExecution = args.SdkVersionExecution,
				WatchToken = args.WatchToken
			};
		}
	}

	public class EnviornmentArgs : IMicroserviceArgs
	{
		public string CustomerID => Environment.GetEnvironmentVariable("CID");
		public string ProjectName => Environment.GetEnvironmentVariable("PID");
		public string Host => Environment.GetEnvironmentVariable("HOST");
		public string Secret => Environment.GetEnvironmentVariable("SECRET");
		public string NamePrefix => Environment.GetEnvironmentVariable("NAME_PREFIX") ?? "";
		public string SdkVersionExecution => Environment.GetEnvironmentVariable("BEAMABLE_SDK_VERSION_EXECUTION") ?? "";
		public bool WatchToken => (Environment.GetEnvironmentVariable("WATCH_TOKEN") ?? "") == "true";
		public string SdkVersionBaseBuild => File.ReadAllText(".beamablesdkversion").Trim();
	}
}
