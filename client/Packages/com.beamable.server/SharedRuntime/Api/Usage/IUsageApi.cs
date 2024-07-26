// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407161549
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407161549

using Beamable.Common;
using System;
using System.Threading.Tasks;

namespace Beamable.Server.Api.Usage
{
	/// <summary>
	/// The <see cref="IUsageApi"/> can be used to find facts about the running service instance
	/// </summary>
	public interface IUsageApi
	{
		/// <returns>the latest <see cref="ServiceUsage"/> data.</returns>
		ServiceUsage GetUsage();

		/// <returns>the <see cref="ServiceMetadata"/> d</returns>
		ServiceMetadata GetMetadata();

		/// <summary>
		/// A function used when the Microservice starts up initialize the usage api.
		/// </summary>
		/// <returns></returns>
		Task Init();
	}

	[Serializable]
	public struct ServiceUsage
	{
		/// <summary>
		/// the average cpu usage %. It is "observed", because it may not be accurate to what Amazon Web Services things is true. 
		/// </summary>
		public double observedCpuAverage;

		/// <summary>
		/// the average memory usage %
		/// </summary>
		public double memoryAverage;

		/// <summary>
		/// the latest cpu usage %. It may not be accurate to what AWS uses.
		/// </summary>
		public double latestCpuUsage;

		/// <summary>
		/// The latest mem usage %
		/// </summary>
		public double latestMemoryUsage;
	}

	[Serializable]
	public struct ServiceMetadata
	{
		/// <summary>
		/// Where is the service running?
		/// </summary>
		public ServiceEnvironment environment;

		/// <summary>
		/// A unique id for this instance
		/// </summary>
		public string instanceId;
	}

	public enum ServiceEnvironment
	{
		LocalStandalone,
		LocalDocker,
		Deployed
	}
}
