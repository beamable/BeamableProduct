using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Server.Common;

namespace Beamable.Server
{
   public class ServiceMethodCollection
   {
      private readonly IEnumerable<ServiceMethod> _methods;
      private Dictionary<string, ServiceMethod> _pathToMethod;

      public ServiceMethodCollection(IEnumerable<ServiceMethod> methods)
      {
         _methods = methods;
         _pathToMethod = methods.ToDictionary(method => method.Path);
      }

      public IEnumerable<ServiceMethod> Methods => _methods.ToList();

      public async Task<string> Handle(RequestContext ctx, string path, IParameterProvider parameterProvider)
      {
         BeamableSerilogProvider.LogContext.Value.Debug($"Handling {path}");
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

            var output = method.Execute(ctx, parameterProvider);
            var result = await output;
            BeamableSerilogProvider.LogContext.Value.Verbose("Method finished with {result}", result);

            var serializedResult = method.ResponseSerializer.SerializeResponse(ctx, result);
            return serializedResult;
         }
         else
         {
            BeamableSerilogProvider.LogContext.Value.Warning($"No method available for {path}");

            throw new UnhandledPathException(path);
         }
      }
   }
}
