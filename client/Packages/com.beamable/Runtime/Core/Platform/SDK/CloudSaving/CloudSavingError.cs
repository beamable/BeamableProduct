using System;

namespace Beamable.Api.CloudSaving
{
   public class CloudSavingError : Exception
   {
      public CloudSavingError(string message, Exception inner) : base(message, inner)
      {
      }
   }
}