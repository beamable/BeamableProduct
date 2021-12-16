namespace Beamable.Server.Editor.DockerCommands
{
   public class FollowLogCommand : DockerCommand
   {
      private readonly IDescriptor _descriptor;
      public string ContainerName { get; }


      public FollowLogCommand(IDescriptor descriptor)
      {
         _descriptor = descriptor;
         ContainerName = descriptor.ContainerName;
      }

      protected override void HandleStandardOut(string data)
      {
         if (!MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
         {
            base.HandleStandardOut(data);
         }
      }

      protected override void HandleStandardErr(string data)
      {
         if (!MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
         {
            base.HandleStandardErr(data);
         }
      }

      public override string GetCommandString()
      {
         return $"{DockerCmd} logs {ContainerName} -f --since 0m";
      }
   }
}
