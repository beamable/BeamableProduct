using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Server.Common;
using System.Diagnostics;

namespace Beamable.Server
{
   public class ServiceMethodCollection
   {
      private readonly IEnumerable<ServiceMethod> _methods;
      private readonly IActivityProvider _activityProvider;
      private Dictionary<string, ServiceMethod> _pathToMethod;

      public ServiceMethodCollection(IEnumerable<ServiceMethod> methods, IActivityProvider activityProvider)
      {
         _methods = methods;
         _activityProvider = activityProvider;
         _pathToMethod = methods.ToDictionary(method => method.Path);
      }

      public IEnumerable<ServiceMethod> Methods => _methods.ToList();

      public async Task<string> Handle(RequestContext ctx, string path, IParameterProvider parameterProvider)
      {

         BeamableSerilogProvider.LogContext.Value.Debug("Handling {path}", path);
         if (_pathToMethod.TryGetValue(path, out var method) )
         {
            if (!ctx.HasScopes(method.RequiredScopes))
            {
               throw new MissingScopesException(ctx.Scopes);
            }

            // Required Auth User Check
            if (ctx.UserId == 0 && method.RequireAuthenticatedUser)
            {
               throw new UnauthorizedUserException(method.Path);
            }

            object result = null;
            using (var _activity = _activityProvider.StartActivity(path + OTElConstants.ACT_CLIENT_CALLABLE))
            {
	            var output = method.Execute(ctx, parameterProvider);
	            result = await output;
	            _activity.SetStatus(ActivityStatusCode.Ok, "Finished");
            }
            BeamableSerilogProvider.LogContext.Value.Debug("Method finished with {result}", result);

            using var serializeActivity = _activityProvider.StartActivity(OTElConstants.ACT_SERIALIZE);
            var serializedResult = method.ResponseSerializer.SerializeResponse(ctx, result);
            return serializedResult;
         }
         else
         {
            BeamableSerilogProvider.LogContext.Value.Warning("No method available for {path}", path);

            throw new UnhandledPathException(path);
         }
      }
   }
}
