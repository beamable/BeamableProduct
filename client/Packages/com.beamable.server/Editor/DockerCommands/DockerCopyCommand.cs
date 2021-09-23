using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
   public class DockerCopyCommand : DockerCommandReturnable<bool>
   {
      private readonly IDescriptor _descriptor;
      private readonly string _containerPath;
      private readonly string _host;
      private bool _copyingOutOfContainer;

      public DockerCopyCommand(IDescriptor descriptor, string containerPath, string host)
      {
         _descriptor = descriptor;
         _containerPath = containerPath;
         _host = host;
         _copyingOutOfContainer = true;
         WriteCommandToUnity = true;
         WriteLogToUnity = true;
      }

      public DockerCopyCommand(string host, IDescriptor descriptor, string containerPath)
      {
         _descriptor = descriptor;
         _containerPath = containerPath;
         _host = host;
         WriteCommandToUnity = true;
         WriteLogToUnity = true;
      }

      public override string GetCommandString()
      {
         var containerPart = $"{_descriptor.ContainerName}:{_containerPath}";
         var cpStr = _copyingOutOfContainer ? $"{containerPart} {_host}" : $"{_host} {containerPart}";
         return $"{DockerCmd} cp {cpStr}";
      }

      protected override void Resolve()
      {
         Promise.CompleteSuccess(true);
      }
   }
}