using Task = Microsoft.Build.Utilities.Task;

namespace microservice.MsBuildTasks;

public class GenerateClient : Task
{
	public override bool Execute()
	{
		Log.LogMessage("Hello world!");
		return true;
	}
}