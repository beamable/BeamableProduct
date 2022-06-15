
using System;

namespace UnityEngine
{
   public interface IDebug
   {
      void Assert(bool assertion);
      void Log(string info);
      void Log(string info, params object[] args);
      void LogWarning(string warning);
      void LogWarning(string warning, params object[] args);
      void LogException(Exception ex);
      void LogError(Exception ex);
      void LogError(string error);
      void LogError(string error, params object[] args);
      void LogFormat(string format, params object[] args);
   }
   public class Debug
   {
      public static IDebug Instance;
      public static void Assert(bool assertion) => Instance.Assert(assertion);

      public static void Log(string info) => Instance.Log(info);

      public static void Log(string info, params object[] args) => Instance.Log(info, args);
      public static void LogWarning(string warning) => Instance.LogWarning(warning);

      public static void LogWarning(string warning, params object[] args) => Instance.LogWarning(warning, args);

      public static void LogException(Exception ex) => Instance.LogException(ex);

      public static void LogError(Exception ex) => Instance.LogError(ex);
      public static void LogError(string error) => Instance.LogError(error);

      public static void LogError(string error, params object[] args) => Instance.LogError(error, args);
      public static void LogFormat(string format, params object[] args) => Instance.LogFormat(format, args);
   }
}