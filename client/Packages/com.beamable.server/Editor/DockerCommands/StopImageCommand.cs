using Beamable.Common;

namespace Beamable.Server.Editor.DockerCommands
{
   public class StopImageReturnableCommand : DockerCommandReturnable<bool>
   {
      public string ContainerName { get; set; }

      public StopImageReturnableCommand(MicroserviceDescriptor descriptor)
      {
         ContainerName = descriptor.ContainerName;
         UnityLogLabel = $"Docker Stop {descriptor.Name}";
      }

      public override string GetCommandString()
      {
         var command = $"{DockerCmd} stop {ContainerName}";
         return command;
      }

      protected override void Resolve()
      {
         Promise?.CompleteSuccess(true);
      }
   }
   public class StopImageCommand : StopImageReturnableCommand
   {
      public StopImageCommand(MicroserviceDescriptor descriptor) : base(descriptor)
      {
      }

      public new Promise<bool> Start()
      {
         return Start(null);
      }
   }
}