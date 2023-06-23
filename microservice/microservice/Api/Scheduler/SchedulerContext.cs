using Beamable.Common.Scheduler;

namespace Beamable.Server.Api.Scheduler;

public class SchedulerContext : IBeamSchedulerContext
{
	public string Cid { get; }
	public string Pid { get; }
	public string Prefix { get; }
	public string ServiceName { get; }

	public SchedulerContext(IMicroserviceArgs args, MicroserviceAttribute attribute)
	{
		Cid = args.CustomerID;
		Pid = args.ProjectName;
		Prefix = args.NamePrefix;
		ServiceName = attribute.MicroserviceName;
	}
}
