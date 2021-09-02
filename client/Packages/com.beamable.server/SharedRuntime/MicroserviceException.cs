using System;
using System.Collections.Generic;

namespace Beamable.Server
{
   public class MicroserviceException : Exception
   {
      public int ResponseStatus { get; set; }
      public string Error { get; set; }
      public new string Message { get; set; }

      public MicroserviceException(int responseStatus, string error, string message) : base($"Service error. status=[{responseStatus}] error=[{error}] message=[{message}] ")
      {
         ResponseStatus = responseStatus;
         Error = error;
         Message = message;
      }
   }

   public class MissingScopesException : MicroserviceException
   {
      public MissingScopesException(IEnumerable<string> currentScopes)
      : base(403, "invalidScopes", $"The scopes [{string.Join(",", currentScopes)}] aren't sufficient for the request.")
      {

      }
   }

   public class UnhandledPathException : MicroserviceException
   {
      public UnhandledPathException(string path)
      : base(404, "unhandledRoute", $"The path=[{path}] has no handler")
      {

      }
   }

   public class ParameterCardinalityException : MicroserviceException
   {
      public ParameterCardinalityException(int requiredCount, int actualCount)
      : base(400, "inputParameterFailure", $"Parameter cardinality failure. required={requiredCount} given={actualCount}")
      {

      }
   }

   public class ParameterLegacyException : MicroserviceException
   {
      public ParameterLegacyException()
         : base(400, "inputParameterFailure", $"Parameters could not be resolved due to legacy reasons. Please don't use the parameter name, \"payload\". Consider using the [Parameter] attribute to rename the parameter. ")
      {

      }
   }

   public class ParameterMissingRequiredException : MicroserviceException
   {
      public ParameterMissingRequiredException(string missingParameterName)
         : base(400, "inputParameterFailure", $"Parameter requires property={missingParameterName}")
      {

      }
   }

   public class ParameterNullException : MicroserviceException
   {
      public ParameterNullException()
         : base(400, "inputParameterFailure", $"Parameters payload cannot be null. Use an empty array for no parameters.")
      {

      }
   }
}