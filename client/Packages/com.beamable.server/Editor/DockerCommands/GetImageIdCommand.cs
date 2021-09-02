using System;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Platform.SDK;

namespace Beamable.Server.Editor.DockerCommands
{
   public class GetImageIdCommand : DockerCommandReturnable<string>
   {
      public string ImageName { get; }

      public GetImageIdCommand(MicroserviceDescriptor descriptor)
      {
         ImageName = descriptor.ImageName;
      }
      public override string GetCommandString()
      {
         return $"{DockerCmd} images -q {ImageName}";
      }

      protected override void Resolve()
      {
         var imageId = StandardOutBuffer.Trim();
         Promise.CompleteSuccess(imageId);
      }
   }
}