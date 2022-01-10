using System;

namespace Beamable.Server.Editor
{
   public interface IDescriptor
   {
      string Name { get; }
      string AttributePath { get; }
      Type Type { get; }

      string ContainerName { get; }
      string ImageName { get; }
      ServiceType ServiceType { get; }
   }
}
