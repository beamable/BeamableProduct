using System;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Platform.SDK;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
   public class GetImageIdCommand : DockerCommandReturnable<string>
   {
      public string ImageName { get; }

      public GetImageIdCommand(IDescriptor descriptor)
      {
         ImageName = descriptor.ImageName;
      }
      public override string GetCommandString()
      {
         return $"{DockerCmd} images -q {ImageName}";
      }

      protected override void Resolve()
      {
         if (StandardOutBuffer?.Length > 0)
         {
	         Promise.CompleteSuccess(StandardOutBuffer.Trim());
	         return;
         }

         Debug.LogError($"Failed to get {ImageName} image id. Error buffer: {StandardErrorBuffer}");
         Promise.CompleteSuccess(string.Empty);
      }
   }
}
