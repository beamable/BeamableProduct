using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor.DockerCommands
{
   public class DeleteVolumeCommand : DockerCommandReturnable<Dictionary<string, bool>>
   {
      public string[] Volumes { get; }

      public Dictionary<string, bool> Results;

      public DeleteVolumeCommand(params string[] volumes)
      {
         Volumes = volumes;
         Results = Volumes.ToDictionary(v => v, v => false);
      }

      public DeleteVolumeCommand(StorageObjectDescriptor storage) : this(storage.DataVolume, storage.FilesVolume)
      {

      }

      public override string GetCommandString()
      {
         return $"{DockerCmd} volume rm {string.Join(" ", Volumes)}";
      }

      protected override void HandleStandardErr(string data) => HandleMessage(data);
      protected override void HandleStandardOut(string data) => HandleMessage(data);

      private void HandleMessage(string msg)
      {
         if (msg == null) return;
         msg = msg.Trim();

         var successes = new HashSet<string>();
         foreach (var res in Results)
         {
            if (string.Equals(msg, res.Key))
            {
               successes.Add(res.Key);
            }
         }

         foreach (var success in successes)
         {
            Results[success] = true;
         }
      }

      protected override void Resolve()
      {
         Promise.CompleteSuccess(Results);
      }
   }
}