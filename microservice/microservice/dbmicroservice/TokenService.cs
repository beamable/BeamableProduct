using System;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Common;
using Serilog;

namespace Beamable.Server;

public class TokenService
{
	public static string GetToken(BeamableMicroService service)
	{
		var typeName = service.MicroserviceType.AssemblyQualifiedName.Replace(
			service.MicroserviceType.Name, "Beamable__Change_Token_Class");
		var type = Type.GetType(typeName, true);

		var method = type.GetMethod("GetToken", BindingFlags.Static | BindingFlags.Public);
		var value = method.Invoke(null, new object[] { });
		var token = value?.ToString() ?? throw new Exception("Invalid token" + value);
		return token;
	}

	public static void WatchTokenChange(BeamableMicroService service)
	{
		Log.Debug("Watching internal token");
		try
		{
			string lastToken = GetToken(service);
			Task.Run(async () =>
			{
				while (!service.IsShuttingDown)
				{
					await Task.Delay(250);
					var token = GetToken(service);
					if (!string.Equals(token, lastToken))
					{
						service.RebuildRouteTable();
					}

					lastToken = token;
				}
			});
		}
		catch (Exception ex)
		{
			Log.Fatal("Failed watching internal token");
			Log.Fatal(ex.Message);
		}
	}
}