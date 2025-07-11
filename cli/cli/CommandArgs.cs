using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Dependencies;
using cli.Content;
using cli.Services;
using cli.Services.DeveloperUserManager;
using Microsoft.Extensions.DependencyInjection;
using BeamActivity = Beamable.Server.BeamActivity;

namespace cli;

public abstract class CommandArgs
{
	public T Create<T>() where T : CommandArgs, new()
	{
		var args = new T { Dryrun = Dryrun, Provider = Provider };
		return args;
	}

	public bool Dryrun { get; set; }
	public bool Quiet { get; set; }
	public bool IgnoreStandaloneValidation { get; set; }
	public IServiceProvider Provider { get; set; }

	public IDependencyProvider DependencyProvider => Provider.GetService<IDependencyProvider>();

	public IAuthApi AuthApi => Provider.GetService<IAuthApi>();
	public ConfigService ConfigService => Provider.GetService<ConfigService>();

	public IAppContext AppContext => Provider.GetService<IAppContext>();
	public IRealmsApi RealmsApi => Provider.GetService<IRealmsApi>();
	public IAliasService AliasService => Provider.GetService<IAliasService>();
	public CliRequester Requester => Provider.GetService<CliRequester>();
	public SwaggerService SwaggerService => Provider.GetService<SwaggerService>();
	public BeamoLocalSystem BeamoLocalSystem => Provider.GetService<BeamoLocalSystem>();
	public BeamoService BeamoService => Provider.GetService<BeamoService>();
	public ContentService ContentService => Provider.GetService<ContentService>();
	public ProjectService ProjectService => Provider.GetService<ProjectService>();
	public DeveloperUserManagerService DeveloperUserManagerService => Provider.GetService<DeveloperUserManagerService>();

	public AppLifecycle Lifecycle => Provider.GetService<AppLifecycle>();
	public BeamActivity RootActivity => Provider.GetService<BeamActivity>();

}

