using System;
using System.IO;
using System.Threading.Tasks;
using Beamable.Editor;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
   [System.Serializable]
   public class MicroserviceBuilder
   {
      private bool _isRunning;
      private bool _isBuilding;
      private bool _isStopping;
      private string _lastImageId;

      public bool IsRunning
      {
         get => _isRunning;
         private set
         {
            if (value == _isRunning) return;
            _isRunning = value;
            // XXX: If OnIsRunningChanged is mutated at before delayCall triggers, non-deterministic behaviour could occur
            EditorApplication.delayCall += () => OnIsRunningChanged?.Invoke(value);
         }
      }

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
      public bool HasBuildDirectory => Directory.Exists(Path.GetFullPath(Descriptor.BuildPath));

      public Action<bool> OnIsRunningChanged;
      public Action<bool> OnIsBuildingChanged;
      public Action<string> OnLastImageIdChanged;

      public MicroserviceDescriptor Descriptor { get; private set; }

      private DockerCommand _logProcess, _runProcess;


      public void ForwardEventsTo(MicroserviceBuilder oldBuilder)
      {
         if (oldBuilder == null) return;
         OnIsRunningChanged += oldBuilder.OnIsRunningChanged;
         OnIsBuildingChanged += oldBuilder.OnIsBuildingChanged;
         OnLastImageIdChanged += oldBuilder.OnLastImageIdChanged;
      }

      public async void Init(MicroserviceDescriptor descriptor)
      {
         Descriptor = descriptor;

         IsBuilding = false;
         await CheckIfIsRunning();
         if (IsRunning)
         {
            CaptureLogs();
         }
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
         var command = new BuildImageCommand(Descriptor, includeDebuggingTools);
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

      private async Task TryToRestart()
      {
         if (!IsRunning) return;

         await TryToStop();
         await TryToStart();
      }

      public async Task TryToStop()
      {
         if (!IsRunning) return;
         if (_isStopping) return;

         _isStopping = true;
         try
         {
            var stopProcess = new StopImageReturnableCommand(Descriptor);
            await stopProcess.Start(null);
            IsRunning = false;
         }
         finally
         {
            _isStopping = false;
         }
      }

      public async Task TryToStart()
      {
         // if the service is already running; don't do anything.
         if (IsRunning) return;

         // TODO: Check if there is a local image available...

         var beamable = await EditorAPI.Instance;
         var secret = await beamable.GetRealmSecret();
         var cid = beamable.CustomerView.Cid;

         if (_runProcess != null) return;
         _runProcess = new RunImageCommand(Descriptor, cid, secret, "Debug");

         // TODO: Send messages to /admin/HealthCheck to see if the service is ready to accept traffic.

         _runProcess.OnExit += i =>
         {
            IsRunning = false;
            _runProcess = null;
         };
         IsRunning = true;
         _runProcess?.Start();
      }

      void CaptureLogs()
      {
         _logProcess?.Kill();
         _logProcess = new FollowLogCommand(Descriptor);
         _logProcess.Start();
      }

   }
}