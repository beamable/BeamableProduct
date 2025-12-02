using Beamable.Common.Content;
using Beamable.Content;
using System;

namespace Beamable.Server.Content
{
   public class MicroserviceContentSerializer : ContentSerializer<IContentObject>
   {
      protected override TContent CreateInstance<TContent>()
      {
         return new TContent();
      }

      protected override IContentObject CreateInstanceWithType(Type type)
      {
	      return (IContentObject)Activator.CreateInstance(type);
      }
   }
}
