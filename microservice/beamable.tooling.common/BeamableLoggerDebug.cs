using Beamable.Common;
using UnityEngine;

namespace Beamable.Server
{
#pragma warning disable 1591
   public class BeamableLoggerDebug : IDebug
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

      public void LogFormat(string format, params object[] args) => BeamableLogger.Log(string.Format(format, args));
   }
#pragma warning enable 1591
}
