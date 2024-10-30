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
	/// If this interface is implemented for a Microservice type, and it is registered in the Beamable
	/// Dependency Injection system, then when the Microservice is building, this implementation will be
	/// executed as the Microservice build happens.
	/// It is a place to add in custom logic for builds.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IMicroserviceBuildHook<T> : IMicroserviceBuildHook
		where T : Microservice
	{

	}

	public interface IMicroserviceBuildContext
	{
		/// <summary>
		/// The <see cref="MicroserviceDescriptor"/> being built.
		/// </summary>
		MicroserviceDescriptor Descriptor { get; }

		/// <summary>
		/// A <see cref="IDependencyProvider"/> being used for the build.
		/// </summary>
		IDependencyProvider Provider { get; }

		/// <summary>
		/// Adding a file will add a file from your local Unity project into the final Microservice
		/// docker image. 
		/// </summary>
		/// <param name="srcPath">The source path should be relative to your Unity project. For example, a valid path may be "Assets/test.txt" </param>
		/// <param name="containerPath">The container path is where the file will be placed in the Docker image. It should also include the copied filename. For example, a valid path may be "mydata/test.txt" </param>
		void AddFile(string srcPath, string containerPath);

		/// <summary>
		/// Adding a directory will add a directory from your local Unity project into the final Microservice docker image.
		/// </summary>
		/// <param name="srcPath">The source path should be relative to your Unity project. For example, a valid path may be "Assets/myContent </param>
		/// <param name="containerPath">The container path is where the directory will be placed in the Docker image. It should also include the copied name. For example, a valid path may be "myContent"</param>
		void AddDirectory(string srcPath, string containerPath);

		/// <summary>
		/// Commiting a file assumes that a file is already present in the docker build context.
		/// </summary>
		/// <param name="containerPath">The container path is where the file will be placed in the Docker image. It should also include the copied filename. For example, a valid path may be "mydata/test.txt" </param>
		void CommitFile(string containerPath);
	}

	public class MicroserviceBuildContext : IMicroserviceBuildContext
	{
		public MicroserviceDescriptor Descriptor { get; set; }
		public IDependencyProvider Provider { get; set; }

		public List<FileAddition> FileAdditions { get; set; } = new List<FileAddition>();

		public void AddFile(string srcPath, string containerPath)
		{
			throw new NotSupportedException("Buildhooks are not supported in Beamable Unity 2.0+");
		}

		public void AddDirectory(string srcPath, string containerPath)
		{
			throw new NotSupportedException("Buildhooks are not supported in Beamable Unity 2.0+");
		}

		public void CommitFile(string containerPath)
		{
			throw new NotSupportedException("Buildhooks are not supported in Beamable Unity 2.0+");
		}

		public class FileAddition
		{
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
