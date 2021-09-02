using System;
using Beamable.Common;
using UnityEngine;

namespace Beamable.Server
{
   public class MicroserviceDebug : IDebug
   {
      public void Assert(bool assertion) => BeamableLogger.Assert(assertion);

      public void Log(string info) => BeamableLogger.Log(info);

      public void Log(string info, params object[] args) => BeamableLogger.Log(info, args);
      public void LogWarning(string warning) => BeamableLogger.LogWarning(warning);

      public void LogWarning(string warning, params object[] args) => BeamableLogger.LogWarning(warning, args);

      public void LogException(Exception ex) => BeamableLogger.LogException(ex);

      public void LogError(Exception ex) => BeamableLogger.LogError(ex);
      public void LogError(string error) => BeamableLogger.LogError(error);

      public void LogError(string error, params object[] args) => BeamableLogger.LogError(error, args);
   }
}