using System;
using System.IO;
using System.Threading.Tasks;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;

namespace Beamable.Editor.UI.Model
{
   [System.Serializable]
   public class MicroserviceBuilder : ServiceBuilderBase
   {
      private bool _isBuilding;
      private string _lastImageId;

      public bool IsBuilding
      {
         get => _isBuilding;
         private set
         {
            if (value == _isBuilding) return;
            _isBuilding = value;
            // XXX: If OnIsBuildingChanged is mutated at before delayCall triggers, non-deterministic behaviour could occur
            EditorApplication.delayCall += () => OnIsBuildingChanged?.Invoke(value);
         }
      }

      public string LastBuildImageId
      {
         get => _lastImageId;
         private set
         {
            if (value == _lastImageId) return;
            _lastImageId = value;
            EditorApplication.delayCall += () => OnLastImageIdChanged?.Invoke(value);
         }
      }

      public bool HasImage => IsRunning || LastBuildImageId?.Length > 0;
      public bool HasBuildDirectory => Directory.Exists(Path.GetFullPath(buildPath));

      public Action<bool> OnIsBuildingChanged;
      public Action<string> OnLastImageIdChanged;
      private string buildPath;


      public void ForwardEventsTo(MicroserviceBuilder oldBuilder)
      {
         if (oldBuilder == null) return;
         OnIsRunningChanged += oldBuilder.OnIsRunningChanged;
         OnIsBuildingChanged += oldBuilder.OnIsBuildingChanged;
         OnLastImageIdChanged += oldBuilder.OnLastImageIdChanged;
      }

      public override async void Init(IDescriptor descriptor)
      {
         base.Init(descriptor);
         buildPath = ((MicroserviceDescriptor) descriptor).BuildPath;
         await TryToGetLastImageId();
      }

      public async Task CheckIfIsRunning()
      {
         var checkProcess = new CheckImageReturnableCommand(Descriptor)
         {
            WriteLogToUnity = false, WriteCommandToUnity = false
         };

         IsRunning = await checkProcess.Start(null);
      }


      public async Task TryToBuild(bool includeDebuggingTools)
      {
         if (IsBuilding) return;

         IsBuilding = true;
         var command = new BuildImageCommand((MicroserviceDescriptor)Descriptor, includeDebuggingTools);
         try
         {
            await command.Start(null);
            await TryToGetLastImageId();
         }
         finally
         {
            IsBuilding = false;
         }
      }

      public async Task TryToGetLastImageId()
      {
         var getChecksum = new GetImageIdCommand(Descriptor);
         try
         {
            LastBuildImageId = await getChecksum.Start(null);
         }
         catch (Exception e)
         {
            System.Console.WriteLine(e);
            throw;
         }
      }

      public async Task TryToBuildAndRestart(bool includeDebuggingTools)
      {
         await TryToBuild(includeDebuggingTools);
         await TryToRestart();
      }

      public async Task TryToBuildAndStart(bool includeDebuggingTools)
      {
         await TryToBuild(includeDebuggingTools);
         await TryToStart();
      }
   }
}