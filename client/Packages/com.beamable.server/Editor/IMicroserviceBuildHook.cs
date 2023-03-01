using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;

namespace Beamable.Server.Editor
{
	public interface IMicroserviceBuildHook
	{
		void Execute(IMicroserviceBuildContext ctx);
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IMicroserviceBuildHook<T> : IMicroserviceBuildHook
		where T : Microservice
	{
		
	}

	public interface IMicroserviceBuildContext
	{
		MicroserviceDescriptor Descriptor { get; }
		IDependencyProvider Provider { get; }
		void AddFile(string srcPath, string containerPath);
	}
	
	public class MicroserviceBuildContext : IMicroserviceBuildContext
	{
		public MicroserviceDescriptor Descriptor { get; set; }
		public IDependencyProvider Provider { get; set; }

		public List<FileAddition> FileAdditions { get; set; } = new List<FileAddition>();
		
		public void AddFile(string srcPath, string containerPath)
		{
			FileAdditions.Add(new FileAddition
			{
				srcPath = srcPath,
				containerPath = containerPath
			});
			FileUtils.CopyFile(Descriptor, srcPath, containerPath);
		}

		public class FileAddition
		{
			public string srcPath;
			public string containerPath;
		}
	}

	public static class MicroserviceDescriptorBuildHookExtensions
	{
		public static Type GetMicroserviceBuildHookType(this MicroserviceDescriptor descriptor)
		{
			var interfaceType = typeof(IMicroserviceBuildHook<>);
			return interfaceType.MakeGenericType(descriptor.Type);
		}
	}
}
