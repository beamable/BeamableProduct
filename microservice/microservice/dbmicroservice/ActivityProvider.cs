using System.Diagnostics;

namespace Beamable.Server;


public interface IActivityProvider
{
	Activity StartActivity(string name);
}

public class ActivityProvider : IActivityProvider
{
	public const string NAME = "Beamable.BeamService.Core";

	private ActivitySource _activitySource;

	public string ActivityName => _activitySource.Name;
	public string ActivityVersion => _activitySource.Version;

	public ActivityProvider(string version=null)
	{
		_activitySource = new ActivitySource(NAME, version ?? "0.0.0");

	}

	public Activity StartActivity(string name)
	{
		var activity = _activitySource.StartActivity(name, ActivityKind.Server);
		activity?.SetTag("peer.service", "Microservice");
		return activity;
	}
}

