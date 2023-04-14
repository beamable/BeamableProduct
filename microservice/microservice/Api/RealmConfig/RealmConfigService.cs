using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Swan.Logging;
using System;

namespace Beamable.Server.Api.RealmConfig
{
   public class RealmConfigService : IMicroserviceRealmConfigService
   {
      private readonly IBeamableRequester _requester;
      private readonly MicroserviceAttribute _serviceAttribute;
      private readonly IMicroserviceArgs _args;

      private RealmConfig _config;


      public RealmConfigService(IBeamableRequester requester, SocketRequesterContext ctx, MicroserviceAttribute serviceAttribute, IMicroserviceArgs args)
      {
	      _config = new RealmConfig(new Dictionary<string, RealmConfigNamespaceData>()); // start empty...
	      
         _requester = requester;
         _serviceAttribute = serviceAttribute;
         _args = args;
         ctx.Subscribe<GetRealmConfigResponse>(
	         Constants.Features.Services.REALM_CONFIG_UPDATE_EVENT,
	         cb =>
	         {
		         GetRealmConfigSettings().Then(_ => UpdateLogLevel());
	         });
         GetRealmConfigSettings().Then(_ =>
         {
	         UpdateLogLevel();
         });
      }

      private void UpdateLogLevel()
      {
	      var level = GetLogLevel();
	      MicroserviceBootstrapper.LogLevel.MinimumLevel = level;
      }

      private LogEventLevel GetLogLevel()
      {
	      if (!_config.TryGetValue(Constants.Features.Services.REALM_CONFIG_SERVICE_LOG_NAMESPACE,
		          out var logInfo))
	      {
		      return LogEventLevel.Debug;
	      }

	      var keyName = _serviceAttribute.MicroserviceName;
	      if (!string.IsNullOrEmpty(_args.NamePrefix))
	      {
		      keyName = $"{_args.NamePrefix}_{keyName}";
	      }
	      if (!logInfo.TryGetValue(keyName, out var logLevel))
	      {
		      return LogEventLevel.Debug;
	      }
	      
	      if (MicroserviceBootstrapper.TryParseLogLevel(logLevel, out var serilogLogLevel))
	      {
		      return serilogLogLevel;
	      }

	      return LogEventLevel.Debug;
      }
      
      private Promise<GetRealmConfigResponse> GetRealmConfig()
      {
         return _requester.Request(Method.GET, "basic/realms/config", parser: Parse);
      }

      private GetRealmConfigResponse Parse(string json)
      {
         var realmData = JsonConvert.DeserializeObject<GetRealmConfigResponse>(json);
         return realmData;
      }

      [System.Serializable]
      private class GetRealmConfigResponse
      {
         public Dictionary<string, string> config;
      }

      private static bool TryExtract(KeyValuePair<string, string> kvp, out string nameSpace, out string setting, out string configValue)
      {
	      nameSpace = null;
	      setting = null;
	      var fullKey = kvp.Key;
	      configValue = kvp.Value;
	      if (string.IsNullOrEmpty(fullKey)) return false; // invalid realm key.

	      var keyParts = fullKey.Split('|');
	      if (keyParts.Length != 2) return false; // invalid realm setting key.

	       nameSpace = keyParts[0];
	       setting = keyParts[1];

	       return true;
      }

      private static RealmConfig Add(Dictionary<string, string> patch, RealmConfig existing=null)
      {
	      var nameSpaceToSettings = new Dictionary<string, Dictionary<string, string>>();

	      if (existing != null)
	      {
		      // add in the existing stuff
		      foreach (var kvp in existing)
		      {
			      nameSpaceToSettings[kvp.Key] = new Dictionary<string, string>(kvp.Value);
		      }
	      }

	      foreach (var kvp in patch)
	      {
		      if (!TryExtract(kvp, out var nameSpace, out var setting, out var configValue))
		      {
			      continue;
		      }

		      if (!nameSpaceToSettings.TryGetValue(nameSpace, out var nameSpaceSection))
		      {
			      nameSpaceSection = new Dictionary<string, string>();
		      }

		      nameSpaceSection[setting] = configValue;
		      nameSpaceToSettings[nameSpace] = nameSpaceSection;
	      }
	      return RealmConfig.From(nameSpaceToSettings);
      }
      
      public async Promise<RealmConfig> GetRealmConfigSettings()
      {
	      var latest = await GetRealmConfig();
	      _config = Add(latest.config, null);
	      return _config;
      }
   }
   
}
