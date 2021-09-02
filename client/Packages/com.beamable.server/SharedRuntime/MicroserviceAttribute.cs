using System;

namespace Beamable.Server
{
   [AttributeUsage(AttributeTargets.Class)]
   public class MicroserviceAttribute : Attribute
   {
      public string MicroserviceName { get; }
      public string SourcePath { get; }

      [Obsolete(
         "Any new client build of your game won't require the payload string. Unless you've deployed a client build using Beamable before version 0.11.0, you shouldn't set this")]
      public bool UseLegacySerialization { get; set; } = false;

      public MicroserviceAttribute(string microserviceName, [System.Runtime.CompilerServices.CallerFilePath] string sourcePath="")
      {
         MicroserviceName = microserviceName;
         SourcePath = sourcePath;
      }

      public string GetSourcePath()
      {
         return SourcePath;
      }
   }
}